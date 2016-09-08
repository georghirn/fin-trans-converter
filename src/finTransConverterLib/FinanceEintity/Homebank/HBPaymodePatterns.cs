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
                
                Patterns.OrderBy(i => i.Level);
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

        public uint Level { get; private set; }

        public string AccountingTextPattern { get; private set; }

        public string MemoPattern { get; private set; }
        
        public HBPaymodePattern(string accountingTextPaterns) {
            Level = 2;
            AccountingTextPattern = accountingTextPaterns;
            MemoPattern = null;
        }

        public HBPaymodePattern(string accountingTextPaterns, string memoPattern = null) {
            if(accountingTextPaterns == null) throw new ArgumentNullException("accountingTextPaterns");
            Level = (uint)((memoPattern == null) ? 2 : 1);
            AccountingTextPattern = accountingTextPaterns;
            MemoPattern = memoPattern;
        }

        public static HBPaymodePattern ParseXmlElement(XmlReader reader) {
            string accountingText = null, memo = null;

            while(reader.MoveToNextAttribute()) {
                switch(reader.Name) {
                    case AttrAccountingText: accountingText = reader.Value; break;
                    case AttrMemo: memo = reader.Value; break;
                }
            }

            return (accountingText != null) ? new HBPaymodePattern(accountingText, memo) : null;
        }

        public override string ToString() {
            return String.Format(
                "|-- Level: {0}" + Environment.NewLine + 
                "|-- AccountingTextPattern: {1}" + Environment.NewLine + 
                "--- MemoPattern: {2}", 
                Level, AccountingTextPattern, MemoPattern
            );
        }
    }
}