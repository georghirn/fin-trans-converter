using System;
using System.Globalization;
using FinTransConverterLib.Accounts;

namespace FinTransConverter
{
    public class FinTransConverter
    {
        public static void Main(string[] args)
        {
            var parsedArgs = new FinTransConverterArgs(args, exit: true);
            //Transaction t = new Transaction();
            HelloBankAccount account = new HelloBankAccount(new CultureInfo("de-at"));
            HomeBankAccount hbAccount = new HomeBankAccount();
            
            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Homebank:");
            hbAccount.ReadFrom(parsedArgs.HomebankSettingsFile);
            
            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Payees:");
            foreach (var payee in hbAccount.Payees) {
                Console.WriteLine(payee.ToString());
            }

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Categories:");
            foreach(var category in hbAccount.Categories) {
                Console.WriteLine(category.ToString());
            }

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Assignments:");
            foreach(var assignment in hbAccount.Assignments) {
                Console.WriteLine(assignment.ToString());
            }

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Hello Bank:");
            account.ReadFrom(parsedArgs.SourceFile);

            foreach(var transaction in account.Transactions) {
                Console.WriteLine("Transaction: " + Environment.NewLine + "{0}" + Environment.NewLine, transaction.ToString());
            }
        }
    }
}
