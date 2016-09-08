using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using FinTransConverterLib.Helpers;

namespace FinTransConverterLib.FinanceEntities.Homebank {
    public class HBAssignment {
        public const string XmlTagName = "asg";
        public const string AttrKey = "key";
        public const string AttrFlags = "flags";
        public const string AttrField = "field";
        public const string AttrName = "name";
        public const string AttrPayee = "payee";
        public const string AttrCategory = "category";

        public HBAssignment() {
            IgnoreCase = false;
            FieldToMatch = eConditionFieldType.PostingText;
        }

        public void ParseXmlElement(XmlReader reader, List<HBPayee> payeeList, List<HBCategory> categoriesList) {
            while(reader.MoveToNextAttribute()) {
                switch(reader.Name) {
                    case AttrKey: Key = XmlConvert.ToUInt32(reader.Value); break;
                    case AttrName: Name = reader.Value; break;
                    case AttrFlags: 
                        var flag = XmlConvert.ToUInt16(reader.Value);
                        if(flag == 6) IgnoreCase = false;
                        else /*if(flag == 7)*/ IgnoreCase = true;
                        break;
                    case AttrField: 
                        var fieldVal = XmlConvert.ToUInt16(reader.Value);
                        if(fieldVal == (int)eConditionFieldType.Payee) FieldToMatch = eConditionFieldType.Payee;
                        else /*if(fieldVal == (int)eConditionFieldType.PostingText)*/ FieldToMatch = eConditionFieldType.PostingText;
                        break;
                    case AttrPayee:
                        var payeeKey = XmlConvert.ToUInt32(reader.Value);
                        Payee = payeeList.Where(p => p.Key == payeeKey).FirstOrDefault();
                        break;
                    case AttrCategory:
                        var categoryKey = XmlConvert.ToUInt32(reader.Value);
                        Category = categoriesList.Where(c => c.Key == categoryKey).FirstOrDefault();
                        break;
                }
            }
        }

        public uint Key { get; private set; }
        public string Name { get; private set; }
        public bool IgnoreCase { get; private set; }
        public eConditionFieldType FieldToMatch { get; private set; }
        public HBPayee Payee { get; private set; }
        public HBCategory Category { get; private set; }

        public override string ToString() {
            return String.Format(
                "|-- Key: {0}" + Environment.NewLine + 
                "|-- Name: {1}" + Environment.NewLine + 
                "|-- IgnoreCase: {2}" + Environment.NewLine + 
                "|-- FieldToMatch: {3}" + Environment.NewLine + 
                "|-+ Payee: " + Environment.NewLine + "{4}" + Environment.NewLine + 
                "--+ Category: " + Environment.NewLine + "{5}", 
                Key, Name, (IgnoreCase) ? "true" : "false", FieldToMatch.ToString(), 
                (Payee == null) ? "  --- null" : Payee.ToString().Indent("| "), 
                (Category == null) ? "  --- null" : Category.ToString().Indent("  ")
            );
        }
    }

    public enum eConditionFieldType {
        PostingText = 0,
        Payee = 1
    }
}