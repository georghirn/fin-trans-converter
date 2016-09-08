using System;
using System.Xml;

namespace FinTransConverterLib.FinanceEntities.Homebank {
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
                "|-- Key: {0}" + Environment.NewLine + 
                "--- Name: {1}", 
                Key, Name
            );
        }
    }
}