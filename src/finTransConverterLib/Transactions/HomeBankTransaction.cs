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
        public const uint XmlPosition = 8;
        public const string XmlTagName = "ope";
        public const string XmlAttrDate = "date";
        public const string XmlAttrAmount = "amount";
        public const string XmlAttrPaymode = "paymode";
        public const string XmlAttrPayee = "payee";
        public const string XmlAttrCategory = "category";
        public const string XmlAttrAccount = "account";
        public const string XmlAttrDestinationAccount = "dst_account";
        public const string XmlAttrWording = "wording";
        public const string XmlAttrInfo = "info";
        public const string XmlAttrTags = "tags";
        public const string XmlAttrStatus = "st";
        public const string XmlAttrFlags = "flags";
        public const string XmlAttrKxfer = "kxfer";
        public const string XmlAttrSplitCategoryies = "scat";
        public const string XmlAttrSplitAmounts = "samt";
        public const string XmlAttrSplitMemos = "smem";
        
        public HomeBankTransaction() { 
            SplitTransactions = new List<SplitTransaction>();
            StrongLinkId = -1;
            Flags = 0;
            DestinationAccount = null;
        }

        public DateTime Date { get; private set; }
        public double Amount { get; private set; }
        public ePaymodeType Paymode { get; private set; }
        public HBPayee Payee { get; private set; }
        public HBCategory Category { get; private set; }
        public HBAccount Account { get; private set; }
        public HBAccount DestinationAccount { get; private set; }
        public string Memo { get; private set; }
        public string Info { get; private set; }
        public string[] Tags { get; private set; }
        public eTransactionStatus Status { get; private set; }
        public int Flags { get; private set; }
        public int StrongLinkId { get; private set; }
        public List<SplitTransaction> SplitTransactions { get; private set; }

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
                if(Amount >= 0) Flags |= (int)eTransactionFlags.Income;

                Account = hb.TargetAccount;
                Status = (trans.ValutaDate.Equals(default(DateTime))) ? eTransactionStatus.Cleared : eTransactionStatus.Reconciled;

                var assignment = TryResolveAssignment(trans.Memo, hb);
                if(assignment != null) {
                    Payee = assignment.Payee;
                    Category = assignment.Category;
                }

                Memo = (trans.PaymentReference.Length > 0) ? String.Format("[Ref: {0}] {1}", trans.PaymentReference, trans.Memo) : trans.Memo;
                Info = trans.AccountingText;
                
                // Parse paymode infos from paymode patterns file.
                var pmInfo = TryFindPaymode(feFrom, hb); // Memo and Info have to be set before.
                Paymode = pmInfo.Paymode;

                // Check if paymode type is between accounts and if so do some further processing.
                if(Paymode == ePaymodeType.BetweenAccounts) {
                    StrongLinkId = ++hb.MaxStrongLinkId;
                    Flags |= (int)eTransactionFlags.Split;

                    // Try to find destination account.
                    if(pmInfo.DestinationAccountPattern != null) {
                        DestinationAccount = hb.Accounts.Where((a) => {
                            return (new Regex(pmInfo.DestinationAccountPattern)).Match(a.Name).Success;
                        }).FirstOrDefault();
                    }
                }

                // Save tags and check if there are new one.
                Tags = pmInfo.TagsString.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                List<string> newTagsNames = Tags.Where(tag => hb.Tags.Any(hbTag => hbTag.Name.Equals(tag)) == false).ToList();
                if(newTagsNames.Count() > 0) {
                    var maxKey = hb.Tags.Max(tag => tag.Key);
                    foreach(var newTagName in newTagsNames) hb.Tags.Add(new HBTag(key: ++maxKey, name: newTagName));
                }

                // Not supported for now: scat, samt, smem (split transactions)

                return;
            }

            // All other transaction types are not supported by this class.
            base.ConvertTransaction(t, feFrom, feTo);
        }
        /*

        <account key="1" pos="1" type="1" name="Girokonto Hello Bank" bankname="Hello Bank" initial="0" minimum="-3000" />
        <account key="6" flags="64" pos="6" type="5" name="[type] liabilities / [not in reports]" number="[institute number]" 
                 bankname="[institute name]" initial="0.080000000000000002" minimum="0.040000000000000001" />
        <ope *date="736222" 
             *amount="-14.219999999999999" 
             *account="1" 
             ???dst_account="6" 
             *paymode="5" 
             ---st="1" 
             *payee="2" 
             *category="14" 
             *wording="test" 
             *info="transfer test" 
             *tags="tagtest1 tagtest2" 
             ???kxfer="2" />
        <ope *date="736222" 
             *amount="14.219999999999999" 
             *account="6" 
             ???dst_account="1" 
             *paymode="5" 
             ---flags="2" 
             *payee="2" 
             *category="14" 
             *wording="test" 
             *info="transfer test" 
             *tags="tagtest1 tagtest2" 
             ???kxfer="2" />
        <ope date="736222" 
             amount="25.329999999999998" 
             account="1" 
             paymode="6" 
             flags="258" 
             wording="Einkauf" 
             info="Einkauf" 
             scat="42||62||78||42" 
             samt="1.8||4.2599999999999767||14.220000000000002||5.0500000000000274" 
             smem="Bananen||bsdfg||sdfdd||dfbdf" />
        ???
            dst_account
            kxfer
            scat
            samt
            smem
        ---
            st
            flags
        */
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
        
        private class ParsedPaymodeInfo {
            public ePaymodeType Paymode { get; set; }
            public string DestinationAccountPattern { get; set; }
            public string TagsString { get; set; }
        }

        private ParsedPaymodeInfo TryFindPaymode(IFinanceEntity feFrom, HomeBank hb) {
            ePaymodeType pType = ePaymodeType.Unknown;
            string destAccPattern = null;
            string tagsStr = null;

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

                            if(isMatch) {
                                destAccPattern = p.DestinationAccountPattern;
                                tagsStr = p.TagsString;
                            }

                            return isMatch;
                        }).Count() > 0)
                        .Select(pt => pt.Paymode)
                        .FirstOrDefault() ?? ePaymodeType.Unknown;
                    break;
            }

            return new ParsedPaymodeInfo() {
                Paymode = pType, 
                DestinationAccountPattern = destAccPattern, 
                TagsString = tagsStr
            };
        }
        
        public void ParseXmlElement(XmlReader reader, HomeBank hba) {
            uint accountKey;
            List<HBCategory> splitCategories = new List<HBCategory>();
            List<double> splitAmounts = new List<double>();
            List<string> splitMemos = new List<string>();

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
                        accountKey = XmlConvert.ToUInt32(reader.Value);
                        Account = hba.Accounts.Where(a => a.Key == accountKey).FirstOrDefault();
                        break;
                    case XmlAttrDestinationAccount:
                        accountKey = XmlConvert.ToUInt32(reader.Value);
                        DestinationAccount = hba.Accounts.Where(a => a.Key == accountKey).FirstOrDefault();
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
                        Status = (eTransactionStatus)XmlConvert.ToUInt32(reader.Value);
                        break;
                    case XmlAttrFlags:
                        Flags = XmlConvert.ToInt32(reader.Value);
                        break;
                    case XmlAttrKxfer:
                        StrongLinkId = XmlConvert.ToInt32(reader.Value);
                        break;
                    case XmlAttrSplitCategoryies:
                        splitCategories = reader.Value
                            .Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select((strCatKey) => 
                                hba.Categories.Where(c => c.Key == Convert.ToUInt32(strCatKey))
                                .FirstOrDefault())
                            .ToList();
                        break;
                    case XmlAttrSplitAmounts:
                        splitAmounts = reader.Value
                            .Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select((strAmount) => Convert.ToDouble(strAmount))
                            .ToList();
                        break;
                    case XmlAttrSplitMemos:
                        splitMemos = reader.Value
                            .Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries)
                            .ToList();
                        break;
                }
            }

            if(splitCategories.Count() == splitAmounts.Count() && splitAmounts.Count() == splitMemos.Count()) {
                for(int i = 0; i < splitCategories.Count(); i++) {
                    SplitTransactions.Add(new SplitTransaction(
                        category: splitCategories[i], 
                        amount: splitAmounts[i],
                        memo: splitMemos[i]
                    ));
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

    public class SplitTransaction {
        public HBCategory Category { get; private set; }
        public double Amount  { get; private set; }
        public string Memo  { get; private set; }

        public SplitTransaction(HBCategory category, double amount, string memo) {
            Category = category;
            Amount = amount;
            Memo = memo;
        }
    }

    public enum eTransactionStatus {
        None, 
        Cleared, 
        Reconciled, 
        Remind
    }

    public enum eTransactionFlags {
        OldValid    = 0x001, // bit 0, deprecated since Hombank 5.x
        Income      = 0x002, // bit 1, 
        Auto        = 0x004, // bit 2, scheduled
        Added       = 0x008, // bit 3, tmp flag
        Changed     = 0x010, // bit 4, tmp flag
        OldRemind   = 0x020, // bit 5, deprecated since Hombank 5.x
        Cheq2       = 0x040, // bit 6
        Limit       = 0x080, // bit 7, scheduled
        Split       = 0x100  // bit 8
    }
}
