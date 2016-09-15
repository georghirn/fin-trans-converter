using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using FinTransConverterLib.Helpers;

namespace FinTransConverterLib.FinanceEntities.Homebank {
    public class HBPaymodePatterns {
        public const string XmlTagName = "paymodepatterns";

        public const string AttrPaymode = "type";

        public ePaymodeType Paymode { get; private set; }

        public List<HBPaymodePattern> Patterns { get; private set; }

        public HBPaymodePatterns() {
            Patterns = new List<HBPaymodePattern>();
        }

        public void ParseXmlElement(XmlReader reader) {
            // Firt parse type attribute.
            if(!reader.HasAttributes) {
                throw new InvalidOperationException(String.Format("Failed to parse xml node {0}, could not find the type attribute.", reader.Name));
            }

            reader.MoveToNextAttribute();
            if(reader.Name != AttrPaymode) {
                throw new InvalidOperationException(String.Format("Failed to parse xml node {0}, could not find the type attribute.", reader.Name));
            } else {
                Paymode = Util
                    .GetEnumValues<ePaymodeType>()
                    .Where(e => e.ToString().Equals(reader.Value))
                    .FirstOrDefault();
            }

            // Now parse patterns.
            reader.MoveToElement();
            using(var innerReader = reader.ReadSubtree()) {
                while(innerReader.Read()) {
                    if(innerReader.NodeType == XmlNodeType.Element && innerReader.Name.Equals(HBPaymodePattern.XmlTagName)) {
                        var pmp = HBPaymodePattern.ParseXmlElement(innerReader);
                        if(pmp != null) Patterns.Add(pmp);
                    }
                }
                
                Patterns = Patterns.OrderBy(i => i.Level).ToList();
            }
        }

        public override string ToString() {
            return String.Format(
                "|-- Paymode: {0}" + Environment.NewLine + 
                "--+ Patterns: " + Environment.NewLine + "{1}", 
                Paymode.ToString(), (new Func<List<HBPaymodePattern>, string>((pts) => { 
                    string s = "";
                    foreach(var p in pts) {
                        if(pts.LastOrDefault().Equals(p)) {
                            s = String.Format("{0}--+ Pattern: " + Environment.NewLine + "{1}", s, p.ToString().Indent("  "));
                        } else {
                            s = String.Format(
                                "{0}|-+ Pattern: " + Environment.NewLine + "{1}" + Environment.NewLine + "|" + Environment.NewLine,
                                s, p.ToString().Indent("| "));
                        }
                    }
                    return s; 
                }))(Patterns).Indent("  ")
            );
        }
    }

    public class HBPaymodePattern {
        public const string XmlTagName = "pattern";

        public const string AttrAccountingText = "accountingtext";

        public const string AttrMemo = "memo";

        public const string AttrDestinationAccountPattern = "destination-account-pattern";

        public const string AttrTags = "tags";

        public uint Level { get; private set; }

        public string AccountingTextPattern { get; private set; }

        public string MemoPattern { get; private set; }

        public string DestinationAccountPattern { get; private set; }

        public string TagsString { get; private set; }
        
        public HBPaymodePattern(string accountingTextPaterns, string destAccPattern = null, string tags = null) {
            Level = 2;
            AccountingTextPattern = accountingTextPaterns;
            MemoPattern = null;
            DestinationAccountPattern = destAccPattern;
            TagsString = tags;
        }

        public HBPaymodePattern(string accountingTextPaterns, string memoPattern = null, string destAccPattern = null, string tags = null) {
            if(accountingTextPaterns == null) throw new ArgumentNullException("accountingTextPaterns");
            Level = (uint)((memoPattern == null) ? 2 : 1);
            AccountingTextPattern = accountingTextPaterns;
            MemoPattern = memoPattern;
            DestinationAccountPattern = destAccPattern;
            TagsString = tags;
        }

        public static HBPaymodePattern ParseXmlElement(XmlReader reader) {
            string accountingText = null, memo = null, destAccPattern = null, tags = null;

            while(reader.MoveToNextAttribute()) {
                switch(reader.Name) {
                    case AttrAccountingText: accountingText = reader.Value; break;
                    case AttrMemo: memo = reader.Value; break;
                    case AttrDestinationAccountPattern: destAccPattern = reader.Value; break;
                    case AttrTags: tags = reader.Value; break;
                }
            }

            return (accountingText != null) ? new HBPaymodePattern(accountingText, memo, destAccPattern, tags) : null;
        }

        public override string ToString() {
            return String.Format(
                "|-- Level: {0}" + Environment.NewLine + 
                "|-- AccountingTextPattern: {1}" + Environment.NewLine + 
                "|-- MemoPattern: {2}" + Environment.NewLine + 
                "|-- DestinationAccountPattern: {3}" + Environment.NewLine + 
                "--- TagsString: {4}", 
                Level, AccountingTextPattern, MemoPattern, DestinationAccountPattern, TagsString
            );
        }
    }

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
}