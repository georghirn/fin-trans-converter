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

            if(parsedArgs.OptVersion) {
                Console.WriteLine("Version option not implemented for now.");
                Environment.Exit(1);
            }

            Console.WriteLine("Start conversion with the following settings:");
            parsedArgs.PrintChosenSettings();

            try {
                switch(parsedArgs.ConversionType) {
                    case eConversionType.HelloBankToHomebank:
                        fromEntity = new HelloBank(parsedArgs.FinanceEntity, new CultureInfo("de-at"));
                        toEntity = new HomeBank(parsedArgs.FinanceEntity, new CultureInfo("de-at"), parsedArgs.TargetAccountPattern);
                        break;
                    default:
                        throw new NotSupportedException(
                            String.Format("The conversion type {0} is currently not supported.", 
                            parsedArgs.ConversionType.ToString()));
                }
                
                fromEntity.FileCheckAndReadIfSupported(eFileTypes.Csv, parsedArgs.SourceFile);
                Console.WriteLine("Successfully parsed source file.");

                if(parsedArgs.HomebankSettingsFile != string.Empty) {
                    toEntity.FileCheckAndReadIfSupported(eFileTypes.Xhb, parsedArgs.HomebankSettingsFile);
                    Console.WriteLine("Homebank settings file successfully parsed.");
                }

                if(Path.GetExtension(parsedArgs.TargetFile).Equals(toEntity.SupportedReadFileTypes[eFileTypes.Xhb].Extension)) {
                    toEntity.FileCheckAndReadIfSupported(eFileTypes.Xhb, parsedArgs.TargetFile);
                    Console.WriteLine("Homebank settings file successfully parsed.");
                }

                if(parsedArgs.PaymodePatternsFile != string.Empty) {
                    toEntity.FileCheckAndReadIfSupported(eFileTypes.PaymodePatterns, parsedArgs.PaymodePatternsFile);
                    Console.WriteLine("Paymode patterns file successfully parsed.");
                }

                toEntity.Convert(fromEntity);
                Console.WriteLine("Successfully performed conversion.");
                toEntity.WriteTo(parsedArgs.TargetFile);
                Console.WriteLine("Results written to the target file successfully.");

                if(parsedArgs.OptVerbose) {
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
                }

                Console.WriteLine("Finished");
            } catch(Exception ex) {
                if(parsedArgs.OptVerbose) Console.WriteLine(ex.ToString());
                else Console.WriteLine(ex.Message);
                
                Console.WriteLine("Failed");
                Environment.Exit(1);
            }
            
            Environment.Exit(0);
        }
    }
}
