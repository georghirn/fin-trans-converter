using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CsvHelper;
using CsvHelper.Configuration;
using FinTransConverterLib.Helpers;
using FinTransConverterLib.Transactions;

namespace FinTransConverterLib.FinanceEntities.Homebank {
    public class HomeBank : FinanceEntity {
        // supported read file types
        private static readonly List<FileType> suppReadFileTypes = new List<FileType> { 
            FinanceEntity.PossibleFileTypes[eFileTypes.Xhb], 
            FinanceEntity.PossibleFileTypes[eFileTypes.PaymodePatterns] 
        };

        // supported write file types
        private static readonly List<FileType> suppWriteFileTypes = new List<FileType> { 
            FinanceEntity.PossibleFileTypes[eFileTypes.Csv], 
            FinanceEntity.PossibleFileTypes[eFileTypes.Xhb]
        };

        private CultureInfo culture;

        private List<HomeBankTransaction> duplicates;

        public const string XmlTagName = "homebank";

        public List<HBAccount> Accounts { get; private set; }
        public List<HBPayee> Payees { get; private set; }
        public List<HBCategory> Categories { get; private set; }
        public List<HBAssignment> Assignments { get; private set; }
        public List<HBTag> Tags { get; private set; }
        public List<HBPaymodePatterns> PaymodePatterns { get; private set; }
        public List<HomeBankTransaction> ExistingTransactions { get; private set; }

        public HBAccount TargetAccount { get; private set; }
        private string TargetAccountPattern;

        internal int MaxStrongLinkId { get; set; }
        
        public HomeBank(eFinanceEntityType entityType, CultureInfo ci = null, string accountPattern = null) : 
            base (suppReadFileTypes, suppWriteFileTypes, entityType) {
            culture = ci ?? (ci = CultureInfo.InvariantCulture);
            duplicates = new List<HomeBankTransaction>();
            Payees = new List<HBPayee>();
            Categories = new List<HBCategory>();
            Assignments = new List<HBAssignment>();
            Tags = new List<HBTag>();
            Accounts = new List<HBAccount>();
            PaymodePatterns = new List<HBPaymodePatterns>();
            ExistingTransactions = new List<HomeBankTransaction>();
            TargetAccountPattern = accountPattern ?? string.Empty;
            TargetAccount = null;
        }

        protected override void Read(string path, FileType fileType) {
            using(StreamReader reader = File.OpenText(path)) {
                switch(fileType.Id) {
                    case eFileTypes.Xhb: ParseHombankSettingsFile(reader); break;
                    case eFileTypes.PaymodePatterns: ParsePaymodePatternsFile(reader); break;
                }
            }
        }

        protected override bool Write(string path, FileType fileType) {
            bool success = true;

            switch(fileType.Id) {
                case eFileTypes.Csv: 
                    using(StreamWriter writer = new StreamWriter(File.OpenWrite(path))) {
                        success = WriteCsv(writer);
                    } 
                    break;
                case eFileTypes.Xhb: 
                    success = WriteHombankSettingsFile(path);
                    break;
            }
            
            return success;
        }

        protected override void WriteFailed(string path) {
            // Writecsv file with duplicates.
            string duplicatesCsvFile = String.Format("{0}{1}{2}.duplicates.csv", 
                Path.GetDirectoryName(path), 
                Path.DirectorySeparatorChar, 
                Path.GetFileNameWithoutExtension(path));
            
            using(StreamWriter output = new StreamWriter(File.OpenWrite(duplicatesCsvFile))) {
                using(var writer = new CsvWriter(output)) {
                    HomeBankTransaction.WriteCsvHeader(writer);
                    foreach(var transaction in duplicates) {
                        transaction.WriteCsv(writer, culture);
                        writer.NextRecord();
                    }
                }
            }

            // Write text file with duplicates. This includes all values of a transaction.
            string duplicatesTextFile = String.Format("{0}{1}{2}.duplicates.txt", 
                Path.GetDirectoryName(path), 
                Path.DirectorySeparatorChar, 
                Path.GetFileNameWithoutExtension(path));
            
            using(StreamWriter output = new StreamWriter(File.OpenWrite(duplicatesTextFile))) {
                output.WriteLine(String.Format(
                    "Timestamp: {0}" + Environment.NewLine + 
                    "Duplicates for target: {1}" + Environment.NewLine + 
                    "---------------------------------------------------------------------" + Environment.NewLine + 
                    "Duplicate transactions:", 
                    (new DateTime()).ToString(), path
                ));
                foreach(var transaction in duplicates) {
                    if(duplicates.LastOrDefault().Equals(transaction)) {
                        output.WriteLine(String.Format(
                            "--+ Transaction: " + Environment.NewLine + "{0}", transaction.ToString().Indent("  ")));
                    } else {
                        output.WriteLine(String.Format(
                            "|-+ Transaction: " + Environment.NewLine + "{0}" + Environment.NewLine + "|", 
                            transaction.ToString().Indent("| ")));
                    }
                }
            }
        }

        private bool WriteCsv(TextWriter output) {
            duplicates.Clear();

            using(var writer = new CsvWriter(output)) {
                ConfigureCsv(writer.Configuration);

                HomeBankTransaction.WriteCsvHeader(writer);
                writer.NextRecord();

                foreach(var transaction in Transactions) {
                    if(transaction.IsDuplicate(ExistingTransactions)) {
                        duplicates.Add(transaction as HomeBankTransaction);
                    } else {
                        (transaction as HomeBankTransaction).WriteCsv(writer, culture);
                        writer.NextRecord();
                    }
                }
            }

            return duplicates.Count() <= 0;
        }
        
        private void ConfigureCsv(CsvConfiguration config) {
            config.AllowComments = false;
            config.CultureInfo = culture;
            config.Delimiter = ";";
            config.HasHeaderRecord = true;
            config.IgnorePrivateAccessor = false;
            config.IgnoreReadingExceptions = false;
            config.IgnoreQuotes = false;
            config.IsHeaderCaseSensitive = true;
            config.Quote = '"';
            config.SkipEmptyRecords = true;
            config.TrimFields = true;
            config.TrimHeaders = true;
            config.WillThrowOnMissingField = true;
        }

        private bool WriteHombankSettingsFile(string path) {
            if(TargetAccountPattern == string.Empty) {
                throw new InvalidOperationException(
                    "Could not write to Homebank settings file, because of missing a valid target account pattern.");
            }

            if(TargetAccount == null) {
                throw new InvalidOperationException(String.Format(
                    "Could not write to Homebank settings file, because there is no account matching" + Environment.NewLine + 
                    "with the target account pattern: {0}", TargetAccountPattern
                ));
            }

            duplicates.Clear();            
            XmlDocument doc = new XmlDocument();
            using(StreamReader xmlReader = new StreamReader(File.OpenRead(path))) {
                doc.Load(xmlReader);
            }

            XmlNodeList rootList = doc.GetElementsByTagName(XmlTagName);
            if(rootList.Count > 0) {
                XmlNode homebank = rootList[0];
                XmlNodeList homebankChilds = homebank.ChildNodes;

                // Write new tags if there are new one.
                if(Tags.Where(tag => tag.FromXml == false).Count() > 0) {
                    XmlNode previousNode = null;

                    XmlNodeList tags = doc.GetElementsByTagName(HBTag.XmlTagName);
                    if(tags.Count > 0) previousNode = tags[tags.Count - 1];
                    else {
                        XmlNodeList categories = doc.GetElementsByTagName(HBCategory.XmlTagName);
                        if(categories.Count > 0) previousNode = categories[categories.Count - 1];
                        else {
                            XmlNodeList payees = doc.GetElementsByTagName(HBPayee.XmlTagName);
                            if(payees.Count > 0) previousNode = payees[payees.Count - 1];
                            else {
                                XmlNodeList accounts = doc.GetElementsByTagName(HBAccount.XmlTagName);
                                if(accounts.Count > 0) previousNode = accounts[accounts.Count - 1];
                                else {
                                    XmlNodeList props = doc.GetElementsByTagName("properties");
                                    if(props.Count > 0) previousNode = props[props.Count - 1];
                                    else previousNode = homebank.FirstChild;
                                }
                            }
                        }
                    }

                    foreach(var tag in Tags) {
                        // Only create new tags.
                        if(tag.FromXml == false) {
                            previousNode = homebank.InsertAfter(tag.CreateXmlElement(doc), previousNode);
                        }
                    }
                }

                // Write new transactions.
                XmlNodeList xmlTransactions = doc.GetElementsByTagName(HomeBankTransaction.XmlTagName);
                XmlNode current = null, previous;
                HomeBankTransaction hbTransaction;

                if(xmlTransactions.Count > 0) current = xmlTransactions[0];
                else current = homebank.LastChild;
                
                foreach(var transaction in Transactions) {
                    hbTransaction = transaction as HomeBankTransaction;
                    
                    // Only add if transaction is not a duplicate.
                    if(hbTransaction.IsDuplicate(ExistingTransactions)) {
                        duplicates.Add(hbTransaction);
                    } else {
                        // Transaction is not a duplicate.
                        previous = hbTransaction.GetPreviousXmlElement(current);
                        previous = homebank.InsertAfter(hbTransaction.CreateXmlElement(doc), previous);

                        if(hbTransaction.Paymode == ePaymodeType.BetweenAccounts) {
                            var linkedTrans = HomeBankTransaction.CreateLinkedTransaction(hbTransaction, culture);
                            // Only add if transaction is not a duplicate.
                            if(linkedTrans.IsDuplicate(ExistingTransactions)) {
                                duplicates.Add(linkedTrans);
                            } else {
                                // Transaction is not a duplicate.
                                var element = linkedTrans.CreateXmlElement(doc);
                                previous = homebank.InsertAfter(element, previous);
                            }
                        }

                        current = previous;
                    }
                }
            }

            using(FileStream fileWriter = File.OpenWrite(path)) {
                byte[] xmlHeader = new UTF8Encoding(true).GetBytes("<?xml version=\"1.0\"?>" + Environment.NewLine);
                fileWriter.Write(xmlHeader, 0, xmlHeader.Length);

                var settings = new XmlWriterSettings() { 
                    OmitXmlDeclaration = true, 
                    Indent = true, 
                    NewLineChars = Environment.NewLine
                };
                
                using(XmlWriter xmlWriter = XmlWriter.Create(fileWriter, settings)) {
                    doc.Save(xmlWriter);
                }
            }

            return duplicates.Count() <= 0;
        }
        
        private void ParsePaymodePatternsFile(TextReader input) {
            using(XmlReader reader = XmlReader.Create(input)) {
                while(reader.Read()) {
                    if(reader.NodeType == XmlNodeType.Element && reader.Name.Equals(HBPaymodePatterns.XmlTagName)) {
                        var pmps = new HBPaymodePatterns();
                        pmps.ParseXmlElement(reader);
                        PaymodePatterns.Add(pmps);
                    }
                }
                
                PaymodePatterns = PaymodePatterns
                    .OrderBy((pt) => pt.Patterns.FirstOrDefault()?.Level ?? uint.MaxValue)
                    .ToList();
            }
        }

        private void ParseHombankSettingsFile(TextReader input) {
            using(XmlReader reader = XmlReader.Create(input)) {
                while(reader.Read()) {
                    if(reader.NodeType == XmlNodeType.Element) {
                        switch(reader.Name) {
                            case HBPayee.XmlTagName: 
                                if(reader.HasAttributes) {
                                    var payee = new HBPayee();
                                    payee.ParseXmlElement(reader);
                                    Payees.Add(payee);
                                    reader.MoveToElement();
                                }
                                break;
                            case HBCategory.XmlTagName:
                                if(reader.HasAttributes) {
                                    var category = new HBCategory();
                                    category.ParseXmlElement(reader, Categories);
                                    Categories.Add(category);
                                    reader.MoveToElement();
                                }
                                break;
                            case HBAssignment.XmlTagName:
                                if(reader.HasAttributes) {
                                    var assignment = new HBAssignment();
                                    assignment.ParseXmlElement(reader, Payees, Categories);
                                    Assignments.Add(assignment);
                                    reader.MoveToElement();
                                }
                                break;
                            case HBAccount.XmlTagName:
                                if(reader.HasAttributes) {
                                    var account = new HBAccount();
                                    account.ParseXmlElement(reader);
                                    Accounts.Add(account);
                                    reader.MoveToElement();
                                }
                                break;
                            case HomeBankTransaction.XmlTagName:
                                if(reader.HasAttributes) {
                                    var existingTransaction = new HomeBankTransaction(culture);
                                    existingTransaction.ParseXmlElement(reader, this);
                                    ExistingTransactions.Add(existingTransaction);
                                    reader.MoveToElement();
                                }
                                break;
                            case HBTag.XmlTagName:
                                if(reader.HasAttributes) {
                                    var tag = new HBTag();
                                    tag.ParseXmlElement(reader);
                                    Tags.Add(tag);
                                    reader.MoveToElement();
                                }
                                break;
                        }
                    }
                }
            }

            // Try to parse taget account.
            if(TargetAccountPattern != string.Empty) {
                TargetAccount = Accounts.Where((a) => {
                    return (new Regex(TargetAccountPattern)).Match(a.Name).Success;
                }).FirstOrDefault();
            }

            // Try to parse maximum strong link id.
            if(ExistingTransactions.Count() > 0) MaxStrongLinkId = ExistingTransactions.Max(t => t.StrongLinkId);
            else MaxStrongLinkId = 0;
        }
        
        public override void Convert(IFinanceEntity finEntity) {
            if(finEntity is HelloBank) {
                foreach(var transaction in finEntity.Transactions) {
                    var helloBankTransaction = transaction as HelloBankTransaction;
                    var homebankTransaction = new HomeBankTransaction(culture);
                    homebankTransaction.ConvertTransaction(helloBankTransaction, finEntity, this);
                    Transactions.Add(homebankTransaction);
                }
                return;
            }

            // All other finance entity types are not supported by this class.
            base.Convert(finEntity);
        }
    }
}