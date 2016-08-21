using System;
using System.Globalization;
using CsvHelper;

namespace FinTransConverterLib.Transactions {
    public sealed class HelloBankTransaction : Transaction {
        public string Iban { get; private set; }
        public int ExtractionNumber { get; private set; }
        public DateTime AccountingDate { get; private set; }
        public DateTime ValutaDate { get; private set; }
        public string PaymentReference { get; private set; }
        public string Currency { get; private set; }
        public double Amount { get; private set; }
        public string AccountingText { get; private set; }
        public string Memo { get; private set; }

        public HelloBankTransaction() : base() {}

        public void ParseCsv(CsvReader reader, CultureInfo culture) {
            Iban                = reader.GetField<string>(0);
            ExtractionNumber    = reader.GetField<int>(1);
            AccountingDate      = reader.GetField<DateTime>(2);
            ValutaDate          = DateTime.ParseExact(reader.GetField<string>(4), "yyyy-MM-dd-HH.mm.ss.FFFFFF", culture.DateTimeFormat);
            PaymentReference    = reader.GetField<string>(5);
            Currency            = reader.GetField<string>(6);
            Amount              = Convert.ToDouble(reader.GetField<string>(7), culture.NumberFormat);
            AccountingText      = reader.GetField<string>(8);
            Memo                = reader.GetField<string>(9);
        }

        //public void 
        public override string ToString() {
            return String.Format(
                "\t-> Iban = {0}" + Environment.NewLine + 
                "\t-> ExtractionNumber = {1}" + Environment.NewLine + 
                "\t-> AccountingDate = {2}" + Environment.NewLine + 
                "\t-> ValutaDate = {3}" + Environment.NewLine + 
                "\t-> PaymentReference = {4}" + Environment.NewLine + 
                "\t-> Currency = {5}" + Environment.NewLine + 
                "\t-> Amount = {6}" + Environment.NewLine + 
                "\t-> AccountingText = {7}" + Environment.NewLine + 
                "\t-> Memo = {8}" + Environment.NewLine, 
                Iban, ExtractionNumber, AccountingDate, ValutaDate, PaymentReference, 
                Currency, Amount, AccountingText, Memo);
        }
    }
}