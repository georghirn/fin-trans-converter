using System;
using System.Collections;
using System.Collections.Generic;
using DocoptNet;

namespace FinTransConverter {
   public class FinTransConverterArgs {
      public readonly string USAGE = 
         "NAME:" + Environment.NewLine + 
         "   FinTransConverter - Converting financial transactions between different formats." + Environment.NewLine + Environment.NewLine + 
         "USAGE:" + Environment.NewLine + 
         "   FinTransConverter SOURCE TARGET [options]" + Environment.NewLine + Environment.NewLine + 
         "OPTIONS:" + Environment.NewLine + 
         "   -h, --help     Display this help and exit." + Environment.NewLine + 
         "   -v, --version  Output version information and exit.";

      private readonly IDictionary<string, DocoptNet.ValueObject> _args;
      
      public FinTransConverterArgs(ICollection<string> argv, bool help = true, object version = null, bool optionsFirst = false, bool exit = false) {
         _args = new Docopt().Apply(USAGE, argv, help, version, optionsFirst, exit);
      }

      public IDictionary<string, DocoptNet.ValueObject> Args {
         get { return _args; }
      }

      public string SourceFile { 
         get { return _args["SOURCE"].ToString(); }
      }

      public string TargetFile {
         get { return _args["TARGET"].ToString(); }
      }

      public bool OptHelp {
         get { return _args["--help"].IsTrue; }
      }

      public bool OptVersion {
         get { return _args["--version"].IsTrue; }
      }
   }
}