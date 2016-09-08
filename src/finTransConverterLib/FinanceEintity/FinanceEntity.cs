using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FinTransConverterLib.Transactions;

namespace FinTransConverterLib.FinanceEntities {
    public interface IFinanceEntity {
        eFinanceEntityType EntityType { get; }

        Dictionary<eFileTypes, FileType> SupportedReadFileTypes { get; }
        Dictionary<eFileTypes, FileType> SupportedWriteFileTypes { get; }
        void ReadFrom(string fileName);
        void WriteTo(string fileName);
    }

    public abstract class FinanceEntity : IFinanceEntity {
        protected static readonly Dictionary<eFileTypes, FileType> PossibleFileTypes = new Dictionary<eFileTypes, FileType>() {
            { eFileTypes.Csv, new FileType() { Id = eFileTypes.Csv, Extension = ".csv", Description = "comma seperated values" } }, 
            { eFileTypes.Xhb, new FileType() { Id = eFileTypes.Xhb, Extension = ".xhb", Description = "Homebank xml settings file" } }, 
            { eFileTypes.PaymodePatterns, new FileType() { Id = eFileTypes.PaymodePatterns, Extension = ".xpmp", Description = "paymode patterns file" } }
        };

        public Dictionary<eFileTypes, FileType> SupportedReadFileTypes { get; private set; }

        public Dictionary<eFileTypes, FileType> SupportedWriteFileTypes { get; private set; }

        public List<Transaction> Transactions { get; protected set; }

        public eFinanceEntityType EntityType { get; private set; }

        public FinanceEntity(List<FileType> suppReadFileTypes, List<FileType> suppWriteFileTypes, eFinanceEntityType entityType = eFinanceEntityType.Unknown) {
            Transactions = new List<Transaction>();
            SupportedReadFileTypes = new Dictionary<eFileTypes, FileType>();
            SupportedWriteFileTypes = new Dictionary<eFileTypes, FileType>();
            EntityType = entityType;

            foreach(var fileType in suppReadFileTypes) SupportedReadFileTypes.Add(fileType.Id, fileType);
            foreach(var fileType in suppWriteFileTypes) SupportedWriteFileTypes.Add(fileType.Id, fileType);
        }

        public void ReadFrom(string fileName) {
            var fileExt = Path.GetExtension(fileName);
            if(fileExt.Equals(String.Empty)) fileExt = PossibleFileTypes[eFileTypes.Csv].Extension;
            var fileType = SupportedReadFileTypes
                .Where(i => i.Value.Extension == fileExt )
                .Select(i => i.Value)
                .FirstOrDefault();

            if(EqualityComparer<FileType>.Default.Equals(fileType, default(FileType))) {
                throw new NotSupportedException(String.Format(
                    "The read file type \"{0}\" is not supported by this instance.", fileExt));
            }

            using(StreamReader reader = File.OpenText(fileName)) {
                Read(reader, fileType);
            }
        }

        protected abstract void Read(TextReader input, FileType fileType);

        public void WriteTo(string fileName) {
            var fileExt = Path.GetExtension(fileName);
            if(fileExt.Equals(String.Empty)) fileExt = PossibleFileTypes[eFileTypes.Csv].Extension;
            var fileType = SupportedWriteFileTypes
                .Where(i => i.Value.Extension == fileExt )
                .Select(i => i.Value)
                .FirstOrDefault();

            if(EqualityComparer<FileType>.Default.Equals(fileType, default(FileType))) {
                throw new NotSupportedException(String.Format(
                    "The write file type \"{0}\" is not supported by this instance.", fileExt));
            }

            using(StreamWriter writer = new StreamWriter(File.OpenWrite(fileName))) {
                Write(writer, fileType);
            }
        }

        protected abstract void Write(TextWriter output, FileType fileType);
    }

    public class FileType : IEquatable<FileType> {
        public eFileTypes Id { get; set; }
        public string Extension { get; set; }
        public string Description { get; set; }

        public bool Equals(FileType other) {
            if(other == null) return false;
            return Id == other.Id && 
                Extension.Equals(other.Extension) && 
                Description.Equals(other.Description);
        }

        public override bool Equals (object obj) {
            if(GetType() != obj.GetType()) return false;
            return Equals(obj as FileType);
        }
        
        public override int GetHashCode() {
            return new {
                Id, 
                Extension, 
                Description
            }.GetHashCode();
        }
    }

    public enum eFileTypes {
        Csv, 
        Xhb, 
        PaymodePatterns
    }

    public enum eFinanceEntityType {
        [Description("The source account type is unknown.")]
        Unknown,
        [Description("The source account type is a check account.")]
        CheckAccount, 
        [Description("The source account type is a deposit account.")]
        DepositAccount, 
        [Description("The source account type is a credit card account.")]
        CreditCardAccount
    }
   
    public enum eConversionType {
        [Description("Converts from the Hello Bank *.csv format to the Hombank *.csv format.")]
        HelloBankToHomebank
    }
}