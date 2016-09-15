using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using FinTransConverterLib.Helpers;

namespace FinTransConverterLib.FinanceEntities.Homebank {
    public class HBCategory {
        public const uint XmlPosition = 5;
        public const string XmlTagName = "cat";
        public const string AttrKey = "key";
        public const string AttrName = "name"; 
        public const string AttrParent = "parent";
        public const string AttrFlags = "flags";

        public HBCategory() {
            Type = eCategoryType.Unknown;
            Parent = null;
        }

        public HBCategory Parent {
            get { return _parent; }
            private set {
                if(_parent == value) return;
                _parent = value;
                if(_parent == null && IsSubcategory) IsSubcategory = false; 
                if(_parent != null && IsSubcategory == false) IsSubcategory = true;
            }
        }
        private HBCategory _parent;
        
        public uint Key { get; private set; }
        public string Name { get; private set; }
        public bool IsSubcategory {
            get { return _isSubcategory; }
            private set {
                if(_isSubcategory == value) return;
                _isSubcategory = value;
                if(_isSubcategory == false && Parent != null) Parent = null;
            }
        }
        private bool _isSubcategory;
        
        public eCategoryType Type { get; private set; }

        public void ParseXmlElement(XmlReader reader, List<HBCategory> listOfOthers) {
            while(reader.MoveToNextAttribute()) {
                switch(reader.Name) {
                    case AttrKey: Key = XmlConvert.ToUInt32(reader.Value); break;
                    case AttrName: Name = reader.Value; break;
                    case AttrFlags: 
                        var flag = XmlConvert.ToUInt16(reader.Value);
                        if(flag == (int)eCategoryType.Expense) Type = eCategoryType.Expense;
                        if(flag == (int)eCategoryType.Income) Type = eCategoryType.Income;
                        break;
                    case AttrParent:
                        var parentKey = XmlConvert.ToUInt32(reader.Value);
                        Parent = listOfOthers.Where(o => o.Key == parentKey).FirstOrDefault();
                        break;
                }
            }
        }

        public override string ToString() {
            return String.Format(
                "|-- Key: {0}" + Environment.NewLine + 
                "|-- Name: {1}" + Environment.NewLine + 
                "|-- IsSubcategory: {2}" + Environment.NewLine + 
                "|-- Type: {3}" + Environment.NewLine + 
                "--+ Parent: " + Environment.NewLine + "{4}", 
                Key, Name, (IsSubcategory) ? "true" : "false", 
                Type.ToString(), (Parent == null) ? "  --- null" : Parent.ToString().Indent("  ")
            );
        }
    }

    public enum eCategoryType {
        Unknown,
        Expense = 1,
        Income = 3
    }
}