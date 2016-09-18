using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using HtmlAgilityPack;
using FinTransConverterLib.Transactions;

namespace FinTransConverterLib.FinanceEntities {
    public class HelloBank : FinanceEntity {
        // supported read file types
        private static readonly List<FileType> suppReadFileTypes = new List<FileType> { 
            FinanceEntity.PossibleFileTypes[eFileTypes.Csv], 
            FinanceEntity.PossibleFileTypes[eFileTypes.Html]
        };

        // supported write file types
        private static readonly List<FileType> suppWriteFileTypes = new List<FileType> { };

        private CultureInfo culture;

        public HelloBank(eFinanceEntityType entityType, CultureInfo ci = null) : 
            base(suppReadFileTypes, suppWriteFileTypes, entityType) { 
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

        private void ParseHtml(TextReader input) {
            // Currently html parsing is only supported for credit card accounts.
            if(EntityType != eFinanceEntityType.CreditCardAccount) return;

            var doc = new HtmlDocument();
            doc.Load(input);

            // Search for the transactions table.
            foreach(var node in doc.DocumentNode.SelectNodes("//table")) {
                if(node.GetAttributeValue("id", null)?.Equals("tblumsatzdata") ?? false) {
                    // Found transactions table, got to table body.
                    var tableBody = node.Element("tbody");
                    if(tableBody != null) {
                        // Read the transactions.
                        var tableRows = tableBody.Elements("tr");
                        foreach(var tableRow in tableRows) {
                            var transaction = new HelloBankTransaction();
                            if(transaction.ParseHtml(tableRow, culture, EntityType) == false) {
                                // The transaction should not be ignored.
                                Transactions.Add(transaction);
                            }
                        }
                    }
                }
                
            }
        }

        protected override void Read(string path, FileType fileType) {
            using(StreamReader reader = File.OpenText(path)) {
                switch(fileType.Id) {
                    case eFileTypes.Csv: ParseCsv(reader); break;
                    case eFileTypes.Html: ParseHtml(reader); break;
                }
            }
        }

        protected override bool Write(string path, FileType fileType) {
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