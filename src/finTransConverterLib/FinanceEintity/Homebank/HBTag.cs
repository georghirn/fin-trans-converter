using System;
using System.Xml;

namespace FinTransConverterLib.FinanceEntities.Homebank {
    public class HBTag {
        public const uint XmlPosition = 6;
        public const string XmlTagName = "tag";
        public const string AttrKey = "key";
        public const string AttrName = "name"; 
        public HBTag() {}

        public HBTag(uint key, string name) {
            Key = key;
            Name = name;
            FromXml = false;
        }

        public bool FromXml { get; private set; }
        public uint Key { get; private set; }
        public string Name { get; private set; }

        public void ParseXmlElement(XmlReader reader) {
            while(reader.MoveToNextAttribute()) {
                switch(reader.Name) {
                    case AttrKey: Key = XmlConvert.ToUInt32(reader.Value); break;
                    case AttrName: Name = reader.Value; break;
                }
            }

            FromXml = true;
        }

        public XmlElement CreateXmlElement(XmlDocument doc) {
            XmlElement elem = doc.CreateElement(XmlTagName);
            
            XmlAttribute attrKey = doc.CreateAttribute(AttrKey);
            attrKey.Value = Key.ToString();
            elem.Attributes.Append(attrKey);

            XmlAttribute attrName = doc.CreateAttribute(AttrName);
            attrName.Value = Name;
            elem.Attributes.Append(attrName);

            return elem;
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