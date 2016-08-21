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
            Console.WriteLine("Hello World!");
            account.ReadFrom(parsedArgs.SourceFile);

            foreach(var transaction in account.Transactions) {
                Console.WriteLine("Transaction: " + Environment.NewLine + "{0}" + Environment.NewLine, transaction.ToString());
            }
        }
    }
}
