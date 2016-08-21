using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using FinTransConverterLib.Transactions;

namespace FinTransConverterLib.Accounts {
    public class HelloBankAccount : Account {
        private CultureInfo culture;

        public HelloBankAccount(CultureInfo ci = null) 
        : base(
            new List<FileType>() { Account.PossibleFileTypes[eFileTypes.Csv] }, // supported read file types
            new List<FileType>() {} // supported write file types
        ) { 
            culture = ci ?? (ci = CultureInfo.InvariantCulture);
        }

        private void ParseCsv(TextReader input) {
            HelloBankTransaction transaction;
            
            var reader = new CsvReader(input);
            ConfigureCsv(reader.Configuration);
            
            while(reader.Read()) {
                transaction = new HelloBankTransaction();
                transaction.ParseCsv(reader, culture);
                Transactions.Add(transaction);
            }
        }

        protected override void Read(TextReader input, FileType fileType) {
            switch(fileType.Id) {
                case eFileTypes.Csv: ParseCsv(input); break;
            }
        }

        protected override void Write(TextWriter output, FileType fileType) {
            // Nothing to do for now.
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