using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using FinTransConverterLib.Helpers;

namespace FinTransConverterLib.FinanceEntities.Homebank {
    public class HBTransactionAssignment {
        public const string Extension = ".xtasg";
        public const string Description = "xml transaction assignments file";
        public const string XmlRootTagName = "transactionassignments";
        public const string XmlTagName = "assignment";
        public const string AttrCategoryKey = "categoryKey";

        public HBCategory Category { get; private set; }
        public List<HBTransactionPattern> Patterns { get; private set; }

        public HBTransactionAssignment() {
            Category = null;
            Patterns = new List<HBTransactionPattern>();
        }

        public void ParseXmlElement(XmlReader reader, HomeBank hb) {
            // First parse category key attribute.
            if(reader.HasAttributes) {
                reader.MoveToNextAttribute();
                if(reader.Name != AttrCategoryKey) {
                    throw new InvalidOperationException(String.Format(
                        "Failed to parse xml node \"{0}\", could not find attribute \"{1}\".", 
                        reader.Name, AttrCategoryKey
                    ));
                } else {
                    var categoryKey = XmlConvert.ToUInt32(reader.Value);
                    Category = hb.Categories
                        .Where(c => c.Key == categoryKey)
                        .FirstOrDefault();
                    
                    if(Category == null) {
                        throw new KeyNotFoundException(String.Format(
                            "Could not find hombank category with the key \"{0}\"", 
                            categoryKey
                        ));
                    }
                }
            }

            // Now parse patterns.
            reader.MoveToElement();
            using(var innerReader = reader.ReadSubtree()) {
                while(innerReader.Read()) {
                    if(innerReader.NodeType == XmlNodeType.Element && innerReader.Name.Equals(HBTransactionPattern.XmlTagName)) {
                        var pattern = new HBTransactionPattern();
                        pattern.ParseXmlElement(innerReader, hb);
                        Patterns.Add(pattern);
                    }
                }
            }
        }

        public bool IsMatch(string searchText, out HBCategory category, out HBPayee payee) {
            category = null;
            payee = null;

            var pattern = Patterns.Where(p => {
                var regex = (p.IgnoreCase) ? new Regex(p.Memo, RegexOptions.IgnoreCase) : new Regex(p.Memo);
                return regex.Match(searchText).Success;
            }).FirstOrDefault(); 

            if(pattern != null) {
                category = Category;
                payee = pattern.Payee;
                return true;
            }

            return false;
        }

        public override string ToString() {
            return String.Format(
                "|-- Category: " + Environment.NewLine + "{0}" + Environment.NewLine + 
                "--+ Patterns: " + Environment.NewLine + "{1}", 
                (Category == null) ? "  --- null" : Category.ToString().Indent("| "), 
                (new Func<List<HBTransactionPattern>, string>((pts) => { 
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

    public class HBTransactionPattern {
        public const string XmlTagName = "pattern";
        public const string AttrPayeeKey = "payeeKey";
        public const string AttrIgnoreCase = "ignore-case";
        public const string AttrMemo = "memo";

        public HBPayee Payee { get; private set; }
        public bool IgnoreCase { get; private set; }
        public string Memo { get; private set; }

        public HBTransactionPattern() { 
            Payee = null;
            IgnoreCase = false;
            Memo = null;
        }

        public void ParseXmlElement(XmlReader reader, HomeBank hb) {
            while(reader.MoveToNextAttribute()) {
                switch(reader.Name) {
                    case AttrPayeeKey:
                        var payeeKey = XmlConvert.ToUInt32(reader.Value);
                        
                        Payee = hb.Payees
                            .Where(p => p.Key == payeeKey)
                            .FirstOrDefault();
                        
                        if(Payee == null) {
                            throw new KeyNotFoundException(String.Format(
                                "Could not find hombank payee with the key \"{0}\"", 
                                payeeKey
                            ));
                        }

                        break;
                    case AttrIgnoreCase:
                        IgnoreCase = XmlConvert.ToBoolean(reader.Value);
                        break;
                    case AttrMemo: 
                        Memo = reader.Value;
                        break;
                }
            }
        }

        public override string ToString() {
            return String.Format(
                "|-- IgnoreCase: {0}" + Environment.NewLine + 
                "|-- Memo: {1}" + Environment.NewLine + 
                "--+ Payee: " + Environment.NewLine + "{2}", 
                IgnoreCase, Memo, 
                (Payee == null) ? "  --- null" : Payee.ToString().Indent("  ")
            );
        }
    }
}