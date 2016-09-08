using System;
using System.Collections.Generic;
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
                case eFinanceEntityType.DepositAccount: break;
                case eFinanceEntityType.CreditCardAccount: pType = ePaymodeType.CreditCard; break;
                case eFinanceEntityType.Unknown: 
                default: break;
            }

            return pType;
        }
        
        public static readonly Dictionary<ePaymodeType, List<string>> PaymodeMatchingPatterns = new Dictionary<ePaymodeType, List<string>>() {
            { ePaymodeType.CreditCard, new List<string>() { } },
            { ePaymodeType.Check, new List<string>() { } },
            { ePaymodeType.Cash, new List<string>() { "Bankomat" } }, 
            { ePaymodeType.Transfer, new List<string>() { "SEPA.*Gutschrift", "SEPA.*Zahlung.*IENT" } }, 
            { ePaymodeType.BetweenAccounts, new List<string>() { } }, 
            { ePaymodeType.DebitCard, new List<string>() { "POS", "SB.*Quick.*Laden", "POS.*Int.*Zahl.*Auftrag", "POS.*Zahlungsauftrag" } }, 
            { ePaymodeType.StandingOrder, new List<string>() { "Dauerauftrag" } }, 
            { ePaymodeType.ElectronicPayment, new List<string>() { } }, 
            { ePaymodeType.Deposit, new List<string>() { "Gutschrift" } }, 
            { ePaymodeType.FiFee, new List<string>() { "Abschluss", "Autom.*Verst.*ndigung", "Verst.*ndigung" } }, 
            { ePaymodeType.Debit, new List<string>() { "SEPA.*Lastschrift" } }
        };
        /*
        Unknown,
        CreditCard = 1,
        Check = 2,
        Cash = 3,
        Transfer = 4,
        BetweenAccounts = 5,
        DebitCard = 6,
        StandingOrder = 7,
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
                    foreach(var tag in tags) str = String.Format("{0} {1}", str, tag);
                    return str;
                }))(Tags),  
                (Payee == null) ? "  --- null" : Payee.ToString().Indent("| "), 
                (Category == null) ? "  --- null" : Category.ToString().Indent("| "), 
                (Account == null) ? "  --- null" : Account.ToString().Indent("  ")
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
    DebitCard = 6,
    StandingOrder = 7,
    ElectronicPayment = 8,
    Deposit = 9,
    FiFee = 10, 
    Debit = 11
}
