using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using CsvHelper;
using FinTransConverterLib.FinanceEntities;
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

        public override bool IsDuplicate(ITransaction t) {
            var trans = t as HomeBankTransaction;
            if(trans == null) return false;

            return Date.Equals(trans.Date) && 
                Amount.Equals(trans.Amount) && 
                Memo.Equals(trans.Memo) && 
                Info.Equals(trans.Info);
        }

        public override void ConvertTransaction(ITransaction t, FinanceEntity feFrom = null, FinanceEntity feTo = null) {
            if(t is HelloBankTransaction) {
                var trans = t as HelloBankTransaction;
                var helloBank = feFrom as HelloBank;
                var hb = feTo as HomeBank;

                Date = trans.ValutaDate;
                Amount = trans.Amount;
                //Paymode

                var assignment = TryResolveAssignment(trans.Memo, hb);
                if(assignment != null) {
                    Payee = assignment.Payee;
                    Category = assignment.Category;
                }

                //Account

                Memo = (trans.PaymentReference.Length > 0) ? String.Format("[Ref: {0}] {1}", trans.PaymentReference, trans.Memo) : trans.Memo;
                Info = trans.AccountingText;

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

        private ePaymodeType TryFindPaymode(FinanceEntity feFrom) {
            ePaymodeType pType = ePaymodeType.Unknown;

            switch(feFrom.EntityType) {
                case eFinanceEntityType.CheckAccount: 
                    break;
                case eFinanceEntityType.DebitAccount: break;
                case eFinanceEntityType.CreditCardAccount: pType = ePaymodeType.CreditCard; break;
                case eFinanceEntityType.Unknown: 
                default: break;
            }

            return pType;
        }
        /*
        Unknown,
        CreditCard = 1,
        Check = 2,
        Cash = 3,
        Transfer = 4,
        BetweenAccounts = 5,
        DirectDebitAuthorityCard = 6,
        AutomaticBillPayment = 7,
        ElectronicPayment = 8,
        Deposit = 9,
        FiFee = 10, 
        Debit = 11
        */
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

        public override string ToString() {
            return String.Format(
                "HomeBankTransaction: " + Environment.NewLine + 
                "\tDate: {0}" + Environment.NewLine + 
                "\tAmount: {1}" + Environment.NewLine + 
                "\tPaymode: {2}" + Environment.NewLine + 
                "\tMemo: {3}" + Environment.NewLine + 
                "\tInfo: {4}" + Environment.NewLine + 
                "\tTags: {5}" + Environment.NewLine + 
                "\tPayee: " + Environment.NewLine + "{6}" + Environment.NewLine + 
                "\tCategory: " + Environment.NewLine + "{7}" + Environment.NewLine + 
                "\tAccount: " + Environment.NewLine + "{8}", 
                Date, Amount, Paymode.ToString(), Memo, Info, (new Func<string[], string>((tags) => { 
                    string str = "";
                    foreach(var tag in tags) str = String.Format("{0} {1}", str, tag);
                    return str;
                }))(Tags),  
                (Payee == null) ? "\t\tnull" : Payee.ToString().Indent("\t"), 
                (Category == null) ? "\t\tnull" : Category.ToString().Indent("\t"), 
                (Account == null) ? "\t\tnull" : Account.ToString().Indent("\t")
            );
        }
    }
}

/*
<ope date="736198" amount="10.18" account="1" paymode="11" st="1" flags="2" payee="2" category="67" wording="Test Buchung" info="test info" tags="bla tag urlpfrumpft" />
date	format must be DD-MM-YY
amount	a number with a '.' or ',' as decimal separator, ex: -24.12 or 36,75
paymode	from 0=none to 10=FI fee
payee	a payee name
category	a full category name (category, or category:subcategory)
memo(wording)	a string
info	a string
tags	tags separated by space, tag is mandatory since v4.5
*/

public enum ePaymodeType {
    Unknown,
    CreditCard = 1,
    Check = 2,
    Cash = 3,
    Transfer = 4,
    BetweenAccounts = 5,
    DirectDebitAuthorityCard = 6,
    AutomaticBillPayment = 7,
    ElectronicPayment = 8,
    Deposit = 9,
    FiFee = 10, 
    Debit = 11
}
