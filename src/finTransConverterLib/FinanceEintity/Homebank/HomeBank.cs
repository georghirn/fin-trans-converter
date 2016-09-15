using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using CsvHelper;
using CsvHelper.Configuration;
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
        
        public HomeBank(CultureInfo ci = null, string accountPattern = null) : base (suppReadFileTypes, suppWriteFileTypes) {
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

        protected override void Read(TextReader input, FileType fileType) {
            switch(fileType.Id) {
                case eFileTypes.Xhb: ParseHombankSettingsFile(input); break;
                case eFileTypes.PaymodePatterns: ParsePaymodePatternsFile(input); break;
            }
        }

        protected override bool Write(TextWriter output, FileType fileType) {
            switch(fileType.Id) {
                case eFileTypes.Csv: return WriteCsv(output);
                case eFileTypes.Xhb: return WriteHombankSettingsFile(output);
            }
            
            return true;
        }

        protected override void WriteFailed(string path) {
            string duplicatesPath = String.Format("{0}{1}{2}.duplicates.csv", 
                Path.GetDirectoryName(path), 
                Path.DirectorySeparatorChar, 
                Path.GetFileNameWithoutExtension(path));
            
            using(StreamWriter output = new StreamWriter(File.OpenWrite(duplicatesPath))) {
                using(var writer = new CsvWriter(output)) {
                    HomeBankTransaction.WriteCsvHeader(writer);
                    foreach(var transaction in duplicates) {
                        transaction.WriteCsv(writer, culture);
                        writer.NextRecord();
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

        private bool WriteHombankSettingsFile(TextWriter output) {
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
            
            return true;
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
                                    var existingTransaction = new HomeBankTransaction();
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
            MaxStrongLinkId = ExistingTransactions.Max(t => t.StrongLinkId);
        }
        
        public override void Convert(IFinanceEntity finEntity) {
            if(finEntity is HelloBank) {
                foreach(var transaction in finEntity.Transactions) {
                    var helloBankTransaction = transaction as HelloBankTransaction;
                    var homebankTransaction = new HomeBankTransaction();
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