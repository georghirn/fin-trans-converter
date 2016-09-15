using System;
using System.Xml;

namespace FinTransConverterLib.FinanceEntities.Homebank {
    public class HBAccount {
        public const uint XmlPosition = 3;
        public const string XmlTagName = "account";
        public const string AttrKey = "key";
        public const string AttrName = "name"; 
        public const string AttrFlags = "flags";
        public const string AttrPos = "pos";
        public const string AttrType = "type";
        public const string AttrNumber = "number";
        public const string AttrBankname = "bankname";
        public const string AttrInitial = "initial";
        public const string AttrMinimum = "minimum";

        public HBAccount() {}

        public uint Key { get; private set; }
        public string Name { get; private set; }
        public string Flags { get; private set; }
        public uint Position { get; private set; }
        public eAccountType Type { get; private set; }
        public string InstituteNumber { get; private set; }
        public string InstituteName { get; private set; }
        public double InitialAmount { get; private set; }
        public double MinimumAmount { get; private set; }
        
        public void ParseXmlElement(XmlReader reader) {
            while(reader.MoveToNextAttribute()) {
                switch(reader.Name) {
                    case AttrKey: Key = XmlConvert.ToUInt32(reader.Value); break;
                    case AttrName: Name = reader.Value; break;
                    case AttrFlags: Flags = reader.Value; break;
                    case AttrPos: Position = XmlConvert.ToUInt32(reader.Value); break;
                    case AttrType: 
                        switch(XmlConvert.ToInt32(reader.Value)) {
                            case (int)eAccountType.Institute: Type = eAccountType.Institute; break;
                            case (int)eAccountType.Cash: Type = eAccountType.Cash; break;
                            case (int)eAccountType.Assets: Type = eAccountType.Assets; break;
                            case (int)eAccountType.CreditCard: Type = eAccountType.CreditCard; break;
                            case (int)eAccountType.Liabilities: Type = eAccountType.Liabilities; break;
                            default: Type = eAccountType.Unknown; break;
                        }
                        break;
                    case AttrNumber: InstituteNumber = reader.Value; break;
                    case AttrBankname: InstituteName = reader.Value; break;
                    case AttrInitial: InitialAmount = XmlConvert.ToDouble(reader.Value); break;
                    case AttrMinimum: MinimumAmount = XmlConvert.ToDouble(reader.Value); break;
                }
            }
        }

        public override string ToString() {
            return String.Format(
                "|-- Key: {0}" + Environment.NewLine + 
                "|-- Name: {1}" + Environment.NewLine + 
                "|-- Flags: {2}" + Environment.NewLine + 
                "|-- Position: {3}" + Environment.NewLine + 
                "|-- Type: {4}" + Environment.NewLine + 
                "|-- InstituteNumber: {5}" + Environment.NewLine + 
                "|-- InstituteName: {6}" + Environment.NewLine + 
                "|-- InitialAmount: {7}" + Environment.NewLine + 
                "--- MinimumAmount: {8}", 
                Key, Name, Flags, Position, Type.ToString(), InstituteNumber, 
                InstituteName, InitialAmount, MinimumAmount
            );
        }
    }

    public enum eAccountType {
        Unknown,
        Institute = 1,
        Cash = 2,
        Assets = 3,
        CreditCard = 4,
        Liabilities = 5
    }
}