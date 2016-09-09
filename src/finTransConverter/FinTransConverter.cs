using System;
using System.IO;
using System.Linq;
using System.Globalization;
using FinTransConverterLib.FinanceEntities;
using FinTransConverterLib.FinanceEntities.Homebank;
using FinTransConverterLib.Helpers;

namespace FinTransConverter {
    public class FinTransConverter {
        public static void Main(string[] args) {
            var parsedArgs = new FinTransConverterArgs(args, exit: true);
            IFinanceEntity fromEntity;
            IFinanceEntity toEntity;

            switch(parsedArgs.ConversionType) {
                case eConversionType.HelloBankToHomebank:
                    fromEntity = new HelloBank(parsedArgs.FinanceEntity, new CultureInfo("de-at"));
                    toEntity = new HomeBank(new CultureInfo("de-at"));
                    break;
                default:
                    throw new NotSupportedException(
                        String.Format("The conversion type {0} is currently not supported.", 
                        parsedArgs.ConversionType.ToString()));
            }
            
            fromEntity.FileCheckAndReadIfSupported(eFileTypes.Csv, parsedArgs.SourceFile);
            toEntity.FileCheckAndReadIfSupported(eFileTypes.Xhb, parsedArgs.HomebankSettingsFile);
            toEntity.FileCheckAndReadIfSupported(eFileTypes.PaymodePatterns, parsedArgs.PaymodePatternsFile);
            toEntity.Convert(fromEntity);
            toEntity.WriteTo(parsedArgs.TargetFile);

            Console.WriteLine("Converted transactions:");
            foreach(var trans in toEntity.Transactions) {
                if(toEntity.Transactions.LastOrDefault().Equals(trans)) {
                    Console.WriteLine(String.Format(
                        "--+ Transaction: " + Environment.NewLine + "{0}", trans.ToString().Indent("  ")));
                } else {
                    Console.WriteLine(String.Format(
                        "|-+ Transaction: " + Environment.NewLine + "{0}" + Environment.NewLine + "|", 
                        trans.ToString().Indent("| ")));
                }
            }
            Console.WriteLine();

            /*Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Homebank:");
            homebank.ReadFrom(parsedArgs.HomebankSettingsFile);
            homebank.ReadFrom(parsedArgs.PaymodePatternsFile);
            
            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Paymode patterns:");
            foreach(var patternsType in homebank.PaymodePatterns) {
                if(homebank.PaymodePatterns.LastOrDefault().Equals(patternsType)) {
                    Console.WriteLine(String.Format(
                        "--+ Paymode pattern: " + Environment.NewLine + "{0}", patternsType.ToString().Indent("  ")));
                } else {
                    Console.WriteLine(String.Format(
                        "|-+ Paymode pattern: " + Environment.NewLine + "{0}" + Environment.NewLine + "|", 
                        patternsType.ToString().Indent("| ")));
                }
            }
            Console.WriteLine();

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Payees:");
            foreach(var payee in homebank.Payees) {
                if(homebank.Payees.LastOrDefault().Equals(payee)) {
                    Console.WriteLine(String.Format(
                        "--+ Payee: " + Environment.NewLine + "{0}", payee.ToString().Indent("  ")));
                } else {
                    Console.WriteLine(String.Format(
                        "|-+ Payee: " + Environment.NewLine + "{0}" + Environment.NewLine + "|", 
                        payee.ToString().Indent("| ")));
                }
            }
            Console.WriteLine();

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Categories:");
            foreach(var category in homebank.Categories) {
                if(homebank.Categories.LastOrDefault().Equals(category)) {
                    Console.WriteLine(String.Format(
                        "--+ Category: " + Environment.NewLine + "{0}", category.ToString().Indent("  ")));
                } else {
                    Console.WriteLine(String.Format(
                        "|-+ Category: " + Environment.NewLine + "{0}" + Environment.NewLine + "|", 
                        category.ToString().Indent("| ")));
                }
            }
            Console.WriteLine();

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Assignments:");
            foreach(var assignment in homebank.Assignments) {
                if(homebank.Assignments.LastOrDefault().Equals(assignment)) {
                    Console.WriteLine(String.Format(
                        "--+ Assignment: " + Environment.NewLine + "{0}", assignment.ToString().Indent("  ")));
                } else {
                    Console.WriteLine(String.Format(
                        "|-+ Assignment: " + Environment.NewLine + "{0}" + Environment.NewLine + "|", 
                        assignment.ToString().Indent("| ")));
                }
            }
            Console.WriteLine();

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Accounts:");
            foreach(var accounts in homebank.Accounts) {
                if(homebank.Accounts.LastOrDefault().Equals(accounts)) {
                    Console.WriteLine(String.Format(
                        "--+ Account: " + Environment.NewLine + "{0}", accounts.ToString().Indent("  ")));
                } else {
                    Console.WriteLine(String.Format(
                        "|-+ Account: " + Environment.NewLine + "{0}" + Environment.NewLine + "|", 
                        accounts.ToString().Indent("| ")));
                }
            }
            Console.WriteLine();

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Transactions:");
            foreach(var trans in homebank.ExistingTransactions) {
                if(homebank.ExistingTransactions.LastOrDefault().Equals(trans)) {
                    Console.WriteLine(String.Format(
                        "--+ Transaction: " + Environment.NewLine + "{0}", trans.ToString().Indent("  ")));
                } else {
                    Console.WriteLine(String.Format(
                        "|-+ Transaction: " + Environment.NewLine + "{0}" + Environment.NewLine + "|", 
                        trans.ToString().Indent("| ")));
                }
            }
            Console.WriteLine();*/

            /*Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Hello Bank:");
            hellobank.ReadFrom(parsedArgs.SourceFile);

            foreach(var transaction in hellobank.Transactions) {
                if(hellobank.Transactions.LastOrDefault().Equals(transaction)) {
                    Console.WriteLine(String.Format(
                        "--+ Transaction: " + Environment.NewLine + "{0}", transaction.ToString().Indent("  ")));
                } else {
                    Console.WriteLine(String.Format(
                        "|-+ Transaction: " + Environment.NewLine + "{0}" + Environment.NewLine + "|", 
                        transaction.ToString().Indent("| ")));
                }
            }
            Console.WriteLine();*/
        }
    }
}
