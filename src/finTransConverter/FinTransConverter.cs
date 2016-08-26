using System;
using System.Globalization;
using FinTransConverterLib.FinanceEntities;

namespace FinTransConverter
{
    public class FinTransConverter
    {
        public static void Main(string[] args)
        {
            var parsedArgs = new FinTransConverterArgs(args, exit: true);
            //Transaction t = new Transaction();
            HelloBank hellobank = new HelloBank(eFinanceEntityType.CheckAccount, new CultureInfo("de-at"));
            HomeBank homebank = new HomeBank();

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Homebank:");
            homebank.ReadFrom(parsedArgs.HomebankSettingsFile);
            
            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Payees:");
            foreach (var payee in homebank.Payees) {
                Console.WriteLine(payee.ToString());
            }

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Categories:");
            foreach(var category in homebank.Categories) {
                Console.WriteLine(category.ToString());
            }

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Assignments:");
            foreach(var assignment in homebank.Assignments) {
                Console.WriteLine(assignment.ToString());
            }

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Accounts:");
            foreach(var accounts in homebank.Accounts) {
                Console.WriteLine(accounts.ToString());
            }

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Transactions:");
            foreach(var trans in homebank.ExistingTransactions) {
                Console.WriteLine(trans.ToString());
            }

            Console.WriteLine("######################################################################################");
            Console.WriteLine("######################################################################################");
            Console.WriteLine("Hello Bank:");
            hellobank.ReadFrom(parsedArgs.SourceFile);

            foreach(var transaction in hellobank.Transactions) {
                Console.WriteLine("Transaction: " + Environment.NewLine + "{0}" + Environment.NewLine, transaction.ToString());
            }
        }
    }
}
