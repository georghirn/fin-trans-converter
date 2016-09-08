using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
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
            FinanceEntity.PossibleFileTypes[eFileTypes.Csv] 
        };

        public List<HBAccount> Accounts { get; private set; }
        public List<HBPayee> Payees { get; private set; }
        public List<HBCategory> Categories { get; private set; }
        public List<HBAssignment> Assignments { get; private set; }
        public List<HBPaymodePatterns> PaymodePatterns { get; private set; }
        public List<HomeBankTransaction> ExistingTransactions { get; private set; }

        public HomeBank() : base (suppReadFileTypes, suppWriteFileTypes) {
            Payees = new List<HBPayee>();
            Categories = new List<HBCategory>();
            Assignments = new List<HBAssignment>();
            Accounts = new List<HBAccount>();
            PaymodePatterns = new List<HBPaymodePatterns>();
            ExistingTransactions = new List<HomeBankTransaction>();
        }

        protected override void Read(TextReader input, FileType fileType) {
            switch(fileType.Id) {
                case eFileTypes.Xhb: ParseHombankSettingsFile(input); break;
                case eFileTypes.PaymodePatterns: ParsePaymodePatternsFile(input); break;
            }
        }

        protected override void Write(TextWriter output, FileType fileType) {
            throw new NotImplementedException();
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
                        }
                    }
                }
            }
        }
    }
}