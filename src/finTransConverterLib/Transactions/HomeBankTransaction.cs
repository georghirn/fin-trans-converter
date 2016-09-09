using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using CsvHelper;
using FinTransConverterLib.FinanceEntities;
using FinTransConverterLib.FinanceEntities.Homebank;
using FinTransConverterLib.Helpers;

namespace FinTransConverterLib.Transactions {
    public class HomeBankTransaction : Transaction {
        public const string XmlTagName = "ope";
        public const string XmlAttrDate = "date";
        public const string XmlAttrAmount = "amount";
        public const string XmlAttrPaymode = "paymode";
        public const string XmlAttrPayee = "payee";
        public const string XmlAttrCategory = "category";
        public const string XmlAttrAccount = "account";
        public const string XmlAttrWording = "wording";
        public const string XmlAttrInfo = "info";
        public const string XmlAttrTags = "tags";
        public const string XmlAttrStatus = "st";
        public const string XmlAttrFlags = "flags";

        public HomeBankTransaction() { }

        public DateTime Date { get; private set; }
        public double Amount { get; private set; }
        public ePaymodeType Paymode { get; private set; }
        public HBPayee Payee { get; private set; }
        public HBCategory Category { get; private set; }
        public HBAccount Account { get; private set; }
        public string Memo { get; private set; }
        public string Info { get; private set; }
        public string[] Tags { get; private set; }

        public override bool IsDuplicate(IEnumerable<ITransaction> transactions) {
            if(transactions == null) throw new ArgumentNullException("transactions");
            bool isDuplicate = false;

            foreach(var transaction in transactions) {
                var trans = transaction as HomeBankTransaction;

                isDuplicate = Date.Date.Equals(trans.Date.Date);
                isDuplicate &= Amount.Equals(trans.Amount);
                isDuplicate &= Memo.Equals(trans.Memo);
                isDuplicate &= Info.Equals(trans.Info);
                
                /*if(Date.Date.Equals(trans.Date.Date)) {
                    Console.WriteLine("Date EQU / Amount {0} / Memo {1} / Info {2} / IsDuplicate: {3}", 
                    Amount.Equals(trans.Amount) ? "EQU" : "NOT", 
                    Memo.Equals(trans.Memo) ? "EQU" : "NOT", 
                    Info.Equals(trans.Info) ? "EQU" : "NOT", 
                    isDuplicate ? "true" : "false");
                }*/

                if(isDuplicate) break;
            }
            
            return isDuplicate;
        }

        public override void ConvertTransaction(ITransaction t, IFinanceEntity feFrom = null, IFinanceEntity feTo = null) {
            if(t is HelloBankTransaction) {
                var trans = t as HelloBankTransaction;
                var helloBank = feFrom as HelloBank;
                var hb = feTo as HomeBank;

                Date = trans.ValutaDate;
                Amount = trans.Amount;

                var assignment = TryResolveAssignment(trans.Memo, hb);
                if(assignment != null) {
                    Payee = assignment.Payee;
                    Category = assignment.Category;
                }

                //Account

                Memo = (trans.PaymentReference.Length > 0) ? String.Format("[Ref: {0}] {1}", trans.PaymentReference, trans.Memo) : trans.Memo;
                Info = trans.AccountingText;
                Paymode = TryFindPaymode(feFrom, hb); // Memo and Info have to be set before.

                //Tags

                return;
            }

            // All other transaction types are not supported by this class.
            base.ConvertTransaction(t, feFrom, feTo);
        }

        private HBPayee TryFindPayee(string searchText, HomeBank hb) {
            if(hb == null) return null;
            return hb.Payees.Find(p => (new Regex(p.Name.Replace(" ", ".*"), RegexOptions.IgnoreCase)).Match(searchText).Success);
        }

        private HBAssignment TryResolveAssignment(string searchText, HomeBank hb) {
            if(hb == null) return null;
            return hb.Assignments.Find(asg => {
                var pattern = asg.Name.Replace(" ", ".*");
                if(asg.IgnoreCase) return (new Regex(pattern, RegexOptions.IgnoreCase)).Match(searchText).Success;
                return (new Regex(pattern)).Match(searchText).Success;
            });
        }
        
        private ePaymodeType TryFindPaymode(IFinanceEntity feFrom, HomeBank hb) {
            ePaymodeType pType = ePaymodeType.Unknown;

            switch(feFrom.EntityType) {
                case eFinanceEntityType.CreditCardAccount: pType = ePaymodeType.CreditCard; break;
                case eFinanceEntityType.CheckAccount: 
                case eFinanceEntityType.DepositAccount: 
                case eFinanceEntityType.Unknown: 
                default: 
                    pType = hb?.PaymodePatterns
                        .Where(pt => pt.Patterns.Where(p => {
                            bool isMatch = true;
                            isMatch &= (new Regex(p.AccountingTextPattern, RegexOptions.IgnoreCase)).Match(Info).Success;
                            if(p.Level == 1) isMatch &= (new Regex(p.MemoPattern, RegexOptions.IgnoreCase)).Match(Memo).Success;
                            return isMatch;
                        }).Count() > 0)
                        .Select(pt => pt.Paymode)
                        .FirstOrDefault() ?? ePaymodeType.Unknown;
                    break;
            }

            return pType;
        }
        
        public void ParseXmlElement(XmlReader reader, HomeBank hba) {
            while (reader.MoveToNextAttribute()) {
                switch (reader.Name) {
                    case XmlAttrDate:
                        Date = (new DateTime()).JulianToDateTime(XmlConvert.ToUInt32(reader.Value));
                        break;
                    case XmlAttrAmount:
                        Amount = XmlConvert.ToDouble(reader.Value);
                        break;
                    case XmlAttrPaymode:
                        Paymode = (ePaymodeType)XmlConvert.ToInt32(reader.Value);
                        break;
                    case XmlAttrPayee:
                        var payeeKey = XmlConvert.ToUInt32(reader.Value);
                        Payee = hba.Payees.Where(p => p.Key == payeeKey).FirstOrDefault();
                        break;
                    case XmlAttrCategory:
                        var categoryKey = XmlConvert.ToUInt32(reader.Value);
                        Category = hba.Categories.Where(c => c.Key == categoryKey).FirstOrDefault();
                        break;
                    case XmlAttrAccount:
                        var accountKey = XmlConvert.ToUInt32(reader.Value);
                        Account = hba.Accounts.Where(a => a.Key == accountKey).FirstOrDefault();
                        break;
                    case XmlAttrWording:
                        Memo = reader.Value;
                        break;
                    case XmlAttrInfo:
                        Info = reader.Value;
                        break;
                    case XmlAttrTags:
                        Tags = reader.Value.Split(new char[] { ' ' });
                        break;
                    case XmlAttrStatus:
                        break;
                    case XmlAttrFlags:
                        break;
                }
            }
        }

        public static void WriteCsvHeader(CsvWriter writer) {
            writer.WriteField("date");
            writer.WriteField("paymode");
            writer.WriteField("info");
            writer.WriteField("payee");
            writer.WriteField("memo");
            writer.WriteField("amount");
            writer.WriteField("category");
            writer.WriteField("tags");
        }

        public void WriteCsv(CsvWriter writer, CultureInfo culture) {
            writer.WriteField(Date.ToString("dd-MM-yy"));
            writer.WriteField((int)Paymode);
            writer.WriteField(Info);
            writer.WriteField(Payee?.Name ?? "");
            writer.WriteField(Memo);
            writer.WriteField(Amount);
            writer.WriteField((Category == null) ? "" : 
                (Category.IsSubcategory) ? String.Format("{0}:{1}", Category.Parent.Name, Category.Name) : Category.Name);
            writer.WriteField((Tags != null && Tags.Length > 0) ? String.Join(" ", Tags) : "");
        }

        public override string ToString() {
            return String.Format(
                "|-- Date: {0}" + Environment.NewLine + 
                "|-- Amount: {1}" + Environment.NewLine + 
                "|-- Paymode: {2}" + Environment.NewLine + 
                "|-- Memo: {3}" + Environment.NewLine + 
                "|-- Info: {4}" + Environment.NewLine + 
                "|-- Tags: {5}" + Environment.NewLine + 
                "|-+ Payee: " + Environment.NewLine + "{6}" + Environment.NewLine + 
                "|-+ Category: " + Environment.NewLine + "{7}" + Environment.NewLine + 
                "--+ Account: " + Environment.NewLine + "{8}", 
                Date, Amount, Paymode.ToString(), Memo, Info, (new Func<string[], string>((tags) => { 
                    string str = "";
                    if(tags != null) foreach(var tag in tags) str = String.Format("{0} {1}", str, tag);
                    return str;
                }))(Tags),  
                (Payee == null) ? "  --- null" : Payee.ToString().Indent("| "), 
                (Category == null) ? "  --- null" : Category.ToString().Indent("| "), 
                (Account == null) ? "  --- null" : Account.ToString().Indent("  ")
            );
        }
    }
}
