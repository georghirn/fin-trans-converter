using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using FinTransConverterLib.Transactions;

namespace FinTransConverterLib.FinanceEntities {
    public class HelloBank : FinanceEntity {
        // supported read file types
        private static readonly List<FileType> suppReadFileTypes = new List<FileType> { 
            FinanceEntity.PossibleFileTypes[eFileTypes.Csv] 
        };

        // supported write file types
        private static readonly List<FileType> suppWriteFileTypes = new List<FileType> { };

        private CultureInfo culture;

        public HelloBank(eFinanceEntityType entityType, CultureInfo ci = null) : base(suppReadFileTypes, suppWriteFileTypes) { 
            culture = ci ?? (ci = CultureInfo.InvariantCulture);
        }

        private void ParseCsv(TextReader input) {
            HelloBankTransaction transaction;
            
            using(var reader = new CsvReader(input)) {
                ConfigureCsv(reader.Configuration);
                
                while(reader.Read()) {
                    transaction = new HelloBankTransaction();
                    transaction.ParseCsv(reader, culture);
                    Transactions.Add(transaction);
                }
            }
        }

        protected override void Read(TextReader input, FileType fileType) {
            switch(fileType.Id) {
                case eFileTypes.Csv: ParseCsv(input); break;
            }
        }

        protected override bool Write(TextWriter output, FileType fileType) {
            // Nothing to do for now.
            return true;
        }

        private void ConfigureCsv(CsvConfiguration config) {
            config.AllowComments = false;
            config.CultureInfo = culture;
            config.Delimiter = ";";
            config.HasHeaderRecord = true;
            config.IgnorePrivateAccessor = false;
            config.IgnoreReadingExceptions = false;
            config.IgnoreQuotes = false;
            config.IsHeaderCaseSensitive = true;
            config.Quote = '"';
            config.SkipEmptyRecords = true;
            config.TrimFields = true;
            config.TrimHeaders = true;
            config.WillThrowOnMissingField = true;
        }
    }
}