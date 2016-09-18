using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using HtmlAgilityPack;
using FinTransConverterLib.Helpers;
using FinTransConverterLib.FinanceEntities;

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
            
            try {
                ValutaDate = DateTime.SpecifyKind(
                    DateTime.ParseExact(
                        reader.GetField<string>(4), 
                        "yyyy-MM-dd-HH.mm.ss.FFFFFF", 
                        culture.DateTimeFormat
                    ), 
                    DateTimeKind.Utc
                );
            } catch(FormatException) {
                ValutaDate = default(DateTime);
            }

            PaymentReference    = reader.GetField<string>(5);
            Currency            = reader.GetField<string>(6);
            Amount              = Convert.ToDouble(reader.GetField<string>(7), culture.NumberFormat);
            AccountingText      = reader.GetField<string>(8);
            Memo                = reader.GetField<string>(9);
        }

        public bool ParseHtml(HtmlNode tableRow, CultureInfo culture, eFinanceEntityType entityType) {
            var cells = tableRow.ChildNodes?.Where(cn => cn.Name.Equals("td"));

            if(cells == null || cells.Count != 6) {
                // Transaction should be ignored.
                return true;
            }

            // Payment reference.
            PaymentReference = HtmlEntity.DeEntitize(cells[0].InnerText);

            // Accounting and valuta date.
            try {
                var date = DateTime.SpecifyKind(
                    DateTime.ParseExact(
                        HtmlEntity.DeEntitize(cells[1].InnerText).RemoveWhitespaces(), 
                        "dd.MM.yyyy", 
                        culture.DateTimeFormat
                    ), 
                    DateTimeKind.Utc
                );

                AccountingDate = date;
                ValutaDate = date;
            } catch(FormatException) {
                AccountingDate = default(DateTime);
                ValutaDate = default(DateTime);
            }
            
            // Memo and accounting text.
            Memo = HtmlEntity.DeEntitize(cells[2].InnerText);

            if(entityType == eFinanceEntityType.CreditCardAccount) {
                // Check if the transaction should be ignored.
                var ignoreRegex = new Regex(
                    ".*Saldo.*aus.*Abrechnung.*vom.*", 
                    RegexOptions.IgnoreCase);
                if(ignoreRegex.Match(Memo).Success) {
                    // Transaction should be ignored.
                    return true;
                }

                // Try to determine accounting text.
                var regex = new Regex("EINZUG.*PAYLIFE", RegexOptions.IgnoreCase);
                if(regex.Match(Memo).Success) {
                    AccountingText = "SEPA credit transfer";
                } else {
                    AccountingText = "credit card";
                }
            }
            
            // Foreign currency amount, will be ignored. 
            //cells[3].InnerText;

            // Conversion rate, will be ignored.
            //cells[4].InnerText;

            // Currency and amount.
            var xmlNumberFormatInfo = culture.NumberFormat.Clone() as NumberFormatInfo;
            xmlNumberFormatInfo.NumberDecimalSeparator = ",";
            xmlNumberFormatInfo.NumberGroupSeparator = ".";

            var amountDivNode = cells[5].Element("div");
            if(amountDivNode != null) {
                var amountChildNodes = amountDivNode.ChildNodes;
                if(amountChildNodes.Count != 2) {
                    // Transaction should be ignored.
                    return true;
                }

                foreach(var node in amountChildNodes) {
                    if(node.NodeType == HtmlNodeType.Element) {
                        Currency = HtmlEntity.DeEntitize(node.InnerText).RemoveWhitespaces();
                    }

                    if(node.NodeType == HtmlNodeType.Text) {
                        Amount = Convert.ToDouble(
                            HtmlEntity.DeEntitize(node.InnerText).RemoveWhitespaces(), 
                            xmlNumberFormatInfo
                        );
                    }
                }
            } else {
                // Transaction should be ignored.
                return true;
            }

            return false; // Transaction should not be ignored.
        }

        public override string ToString() {
            return String.Format(
                "|-- Iban = {0}" + Environment.NewLine + 
                "|-- ExtractionNumber = {1}" + Environment.NewLine + 
                "|-- AccountingDate = {2}" + Environment.NewLine + 
                "|-- ValutaDate = {3}" + Environment.NewLine + 
                "|-- PaymentReference = {4}" + Environment.NewLine + 
                "|-- Currency = {5}" + Environment.NewLine + 
                "|-- Amount = {6}" + Environment.NewLine + 
                "|-- AccountingText = {7}" + Environment.NewLine + 
                "--- Memo = {8}", 
                Iban, ExtractionNumber, AccountingDate, ValutaDate, PaymentReference, 
                Currency, Amount, AccountingText, Memo);
        }

        public override bool IsDuplicate(IEnumerable<ITransaction> transactions) {
            if(transactions == null) throw new ArgumentNullException("transactions");
            bool isDuplicate = false;

            foreach(var transaction in transactions) {
                var trans = transaction as HelloBankTransaction;
                
                isDuplicate = AccountingDate.Equals(trans.AccountingDate) && 
                    ValutaDate.Equals(trans.ValutaDate) && 
                    PaymentReference.Equals(trans.PaymentReference) && 
                    Currency.Equals(trans.Currency) && 
                    Amount.Equals(trans.Amount) && 
                    AccountingText.Equals(trans.AccountingText) && 
                    Memo.Equals(trans.Memo);
                
                if(isDuplicate) break;
            }
            
            return isDuplicate;
        }

        public override void ConvertTransaction(ITransaction t, IFinanceEntity feFrom = null, IFinanceEntity feTo = null) {
            base.ConvertTransaction(t, feFrom, feTo); // Currently no conversion supported by this class.
        }
    }
}