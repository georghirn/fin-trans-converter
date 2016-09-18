using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FinTransConverterLib.Transactions;

namespace FinTransConverterLib.FinanceEntities {
    public interface IFinanceEntity {
        eFinanceEntityType EntityType { get; }
        List<ITransaction> Transactions { get; }

        Dictionary<eFileTypes, FileType> SupportedReadFileTypes { get; }
        Dictionary<eFileTypes, FileType> SupportedWriteFileTypes { get; }
        void ReadFrom(string path);
        void WriteTo(string path);
        void Convert(IFinanceEntity finEntity);
        void FileCheckAndReadIfSupported(eFileTypes fileType, string path);
    }

    public abstract class FinanceEntity : IFinanceEntity {
        protected static readonly Dictionary<eFileTypes, FileType> PossibleFileTypes = new Dictionary<eFileTypes, FileType>() {
            { 
                eFileTypes.Csv, new FileType() { 
                    Id = eFileTypes.Csv, 
                    Extension = ".csv", 
                    Description = "comma seperated values" 
                } 
            }, { 
                eFileTypes.Html, new FileType() { 
                    Id = eFileTypes.Html, 
                    Extension = ".html", 
                    Description = "hyper text markup language" 
                }
            }, { 
                eFileTypes.PaymodePatterns, new FileType() { 
                    Id = eFileTypes.PaymodePatterns, 
                    Extension = ".xpmp", 
                    Description = "paymode patterns file" 
                } 
            }, { 
                eFileTypes.Xhb, new FileType() { 
                    Id = eFileTypes.Xhb, 
                    Extension = ".xhb", 
                    Description = "Homebank xml settings file" 
                } 
            }
        };
        
        public Dictionary<eFileTypes, FileType> SupportedReadFileTypes { get; private set; }

        public Dictionary<eFileTypes, FileType> SupportedWriteFileTypes { get; private set; }

        public List<ITransaction> Transactions { get; protected set; }

        public eFinanceEntityType EntityType { get; private set; }

        public FinanceEntity(List<FileType> suppReadFileTypes, List<FileType> suppWriteFileTypes, eFinanceEntityType entityType = eFinanceEntityType.Unknown) {
            Transactions = new List<ITransaction>();
            SupportedReadFileTypes = new Dictionary<eFileTypes, FileType>();
            SupportedWriteFileTypes = new Dictionary<eFileTypes, FileType>();
            EntityType = entityType;

            foreach(var fileType in suppReadFileTypes) SupportedReadFileTypes.Add(fileType.Id, fileType);
            foreach(var fileType in suppWriteFileTypes) SupportedWriteFileTypes.Add(fileType.Id, fileType);
        }

        public void ReadFrom(string path) {
            var fileExt = Path.GetExtension(path);
            if(fileExt.Equals(String.Empty)) fileExt = PossibleFileTypes[eFileTypes.Csv].Extension;
            var fileType = SupportedReadFileTypes
                .Where(i => i.Value.Extension == fileExt )
                .Select(i => i.Value)
                .FirstOrDefault();

            if(EqualityComparer<FileType>.Default.Equals(fileType, default(FileType))) {
                throw new NotSupportedException(String.Format(
                    "The read file type \"{0}\" is not supported by the {1} instance.", fileExt, this.GetType().Name));
            }

            Read(path, fileType);
        }

        protected abstract void Read(string path, FileType fileType);

        public void WriteTo(string path) {
            var fileExt = Path.GetExtension(path);
            if(fileExt.Equals(String.Empty)) fileExt = PossibleFileTypes[eFileTypes.Csv].Extension;
            var fileType = SupportedWriteFileTypes
                .Where(i => i.Value.Extension == fileExt )
                .Select(i => i.Value)
                .FirstOrDefault();

            if(EqualityComparer<FileType>.Default.Equals(fileType, default(FileType))) {
                throw new NotSupportedException(String.Format(
                    "The write file type \"{0}\" is not supported by the {1} instance.", fileExt, this.GetType().Name));
            }

            if(Write(path, fileType) == false) WriteFailed(path);
        }

        protected abstract bool Write(string path, FileType fileType);

        protected virtual void WriteFailed(string path) {
            // Nothing to do.
        }

        public virtual void Convert(IFinanceEntity finEntity){
            throw new NotSupportedException(
                String.Format("The finance entity type \"{0}\" is not supported.", finEntity.GetType().Name));
        }
        public void FileCheckAndReadIfSupported(eFileTypes fileType, string path) {
            if(File.Exists(path)) {
                if(SupportedReadFileTypes.ContainsKey(fileType)) {
                    ReadFrom(path);
                } else {
                    throw new NotSupportedException(
                        String.Format("The read file type \"{0}\" is not supported by the {1} instance.", 
                        fileType.ToString(), this.GetType().Name));
                }
            } else {
                throw new FileNotFoundException(String.Format("File does not exist: {0}", path), path);
            }
        }
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
        Html,
        PaymodePatterns,
        Xhb
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