using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
//using FinTransConverterLib.Transactions;
using FinTransConverterLib.Helpers;

namespace FinTransConverterLib.Accounts {
    public class HomeBankAccount : Account {
        public List<HBPayee> Payees { get; private set; }
        public List<HBCategory> Categories { get; private set; }
        public List<HBAssignment> Assignments { get; private set; }

        public HomeBankAccount() 
        : base (
            new List<FileType> { Account.PossibleFileTypes[eFileTypes.Xhb] }, // supported read file types 
            new List<FileType> { Account.PossibleFileTypes[eFileTypes.Csv] } // supported write file types
        ) {
            Payees = new List<HBPayee>();
            Categories = new List<HBCategory>();
            Assignments = new List<HBAssignment>();
        }

        protected override void Read(TextReader input, FileType fileType) {
            switch(fileType.Id) {
                case eFileTypes.Xhb: ParseHombankSettingsFile(input); break;
            }
        }

        protected override void Write(TextWriter output, FileType fileType) {
            throw new NotImplementedException();
        }

        private void ParseHombankSettingsFile(TextReader input) {
            using(XmlReader reader = XmlReader.Create(input)) {
                //reader.MoveToContent();
                while(reader.Read()) {
                    if(reader.NodeType == XmlNodeType.Element) {
                        //Console.WriteLine("NodeType: {0}, Name: {1}, Value: {2}", reader.NodeType.ToString(), reader.Name, reader.Value);
                        
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
                        }

                        /*if(reader.HasAttributes) {
                            Console.Write("Attributes of <" + reader.Name + ">: ");
                            while (reader.MoveToNextAttribute()) {
                                Console.Write(" {0}={1}", reader.Name, reader.Value);
                            }
                            Console.WriteLine();
                            // Move the reader back to the element node.
                            reader.MoveToElement();
                        }*/
                    }
                }
            }
        }
    }

    public class HBPayee {
        public const string XmlTagName = "pay";
        public const string AttrKey = "key";
        public const string AttrName = "name"; 
        public HBPayee() {}

        public uint Key { get; private set; }
        public string Name { get; private set; }

        public void ParseXmlElement(XmlReader reader) {
            while(reader.MoveToNextAttribute()) {
                switch(reader.Name) {
                    case AttrKey: Key = XmlConvert.ToUInt32(reader.Value); break;
                    case AttrName: Name = reader.Value; break;
                }
            }
        }

        public override string ToString() {
            return String.Format(
                "Payee: " + Environment.NewLine + 
                "\tKey: {0}" + Environment.NewLine + 
                "\tName: {1}" + Environment.NewLine, 
                Key, Name
            );
        }
    }

    public class HBCategory {
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
            string str = String.Format(
                "Category: " + Environment.NewLine + 
                "\tKey: {0}" + Environment.NewLine + 
                "\tName: {1}" + Environment.NewLine + 
                "\tIsSubcategory: {2}" + Environment.NewLine + 
                "\tType: {3}" + Environment.NewLine + 
                "\tParent: " + Environment.NewLine + "{4}", 
                Key, Name, (IsSubcategory) ? "true" : "false", 
                Type.ToString(), (Parent == null) ? "\t\tnull" : Parent.ToString().Indent("\t")
            );

            if(IsSubcategory) return str.Indent("\t");
            return str;
        }
    }

    public enum eCategoryType {
        Unknown,
        Expense = 1,
        Income = 3
    }
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
                "Assignment: " + Environment.NewLine + 
                "\tKey: {0}" + Environment.NewLine + 
                "\tName: {1}" + Environment.NewLine + 
                "\tIgnoreCase: {2}" + Environment.NewLine + 
                "\tFieldToMatch: {3}" + Environment.NewLine + 
                "\tPayee: " + Environment.NewLine + "{4}" + Environment.NewLine + 
                "\tCategory: " + Environment.NewLine + "{5}", 
                Key, Name, (IgnoreCase) ? "true" : "false", FieldToMatch.ToString(), 
                (Payee == null) ? "\t\tnull" : Payee.ToString().Indent("\t"), 
                (Category == null) ? "\t\tnull" : Category.ToString().Indent("\t")
            );
        }
    }

    public enum eConditionFieldType {
        PostingText = 0,
        Payee = 1
    }
}