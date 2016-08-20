using System;
using FinTransConverterLib;

namespace FinTransConverter
{
    public class FinTransConverter
    {
        public static void Main(string[] args)
        {
            var parsedArgs = new FinTransConverterArgs(args, exit: true);
            Transaction t = new Transaction();
            Console.WriteLine("Hello World!");

            foreach(var argItem in parsedArgs.Args) {
                Console.WriteLine("{0} = {1}", argItem.Key, argItem.Value);
            }
        }
    }
}
