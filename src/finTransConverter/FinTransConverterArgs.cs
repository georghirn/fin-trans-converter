using System;
using System.Collections.Generic;
using System.ComponentModel;
using DocoptNet;
using FinTransConverterLib.Helpers;
using FinTransConverterLib.FinanceEntities;

namespace FinTransConverter {
   public class FinTransConverterArgs {
      public readonly string USAGE;

      private readonly IDictionary<string, DocoptNet.ValueObject> _args;
      
      public FinTransConverterArgs(ICollection<string> argv, bool help = true, object version = null, bool optionsFirst = false, bool exit = false) {
         USAGE = 
            "NAME:" + Environment.NewLine + 
            "   FinTransConverter - Converting financial transactions between different formats." + Environment.NewLine + Environment.NewLine + 
            "USAGE:" + Environment.NewLine + 
            "   FinTransConverter SOURCE TARGET CONVERSIONTYPE [options]" + Environment.NewLine + Environment.NewLine + 
            "DESCRIPTION:" + Environment.NewLine + 
            "   Financial transaction converter converts different transaction formats from one to another." + Environment.NewLine + 
            "   The first parameter is the source file where the transactions to be converted are. The second" + Environment.NewLine +
            "   parameter is the target file in which the converted transactions will be saved. The third " + Environment.NewLine +
            "   parameter defines the conversion type. The following conversion types are supported:" + Environment.NewLine +
            GetConversionTypesDescription() + Environment.NewLine + 
            "OPTIONS:" + Environment.NewLine + 
            "   -h, --help     Display this help and exit." + Environment.NewLine + 
            "   --version      Output version information and exit." + Environment.NewLine + 
            "   -v, --verbose  Be more verbose." + Environment.NewLine + 
            "   --homebank-settings-file=HOMEBANKSETTINGS  Only valid when target conversion is a Hombank format." + Environment.NewLine + 
            "      Use this option to import a Homebank settings file (*.xhb). This will be parsed and used to" + Environment.NewLine + 
            "      refine the conversion, e.g. for automatic transaction assignment. If the target is a Hombank" + Environment.NewLine + 
            "      settings file (*.xhb) then this option will be ignored." + Environment.NewLine + 
            "   --paymode-patterns-file=PAYMODEPATTERNS  Only valid when target conversion is a Homebank format." + Environment.NewLine + 
            "      Use this option to import a paymode patterns file (*.xpmp). This will be parsed and used to" + Environment.NewLine + 
            "      refine the conversion." + Environment.NewLine + 
            "   -a=ACCOUNTPATTERN, --target-account-pattern=ACCOUNTPATTERN Defines the account for the transactions." + Environment.NewLine + 
            "      The account will be parsed from the Hombank settings file with the pattern. Only valid if the " + Environment.NewLine + 
            "      target is a Homebank settings file (*.xhb) or if the --homebank-settings-file option is used." + Environment.NewLine + 
            "   -d, --append-duplicates  Append new duplicates to duplicates file instead of overriding." + Environment.NewLine + 
            "   -t=ACCOUNTTYPE, --account-type=ACCOUNTTYPE  [default: Unknown] The account (finance entity) type," + Environment.NewLine + 
            "      the following types are supported:" + Environment.NewLine + 
            GetFinanceEntityTypesDescription() + Environment.NewLine;
         _args = new Docopt().Apply(USAGE, argv, help, version, optionsFirst, exit);

         try {
            ConversionType = _args["CONVERSIONTYPE"].ToString().ToEnum<eConversionType>();
         } catch(KeyNotFoundException ex) {
             throw new DocoptInputErrorException(String.Format("Invalid conversion type - {0}", ex.Message));
         }

         try {
            FinanceEntity = _args["--account-type"].ToString().ToEnum<eFinanceEntityType>();
         } catch(KeyNotFoundException ex) {
             throw new DocoptInputErrorException(String.Format("Invalid account type - {0}", ex.Message));
         }
      }

      public IDictionary<string, DocoptNet.ValueObject> Args { get { return _args; } }

      public void PrintChosenSettings() {
          Console.WriteLine(
              "  -> source file: {0}" + Environment.NewLine + 
              "  -> target file: {1}" + Environment.NewLine + 
              "  -> conversion type: {2}" + Environment.NewLine + 
              "  -> account type: {3}" + Environment.NewLine + 
              "{4}{5}{6}{7}", 
              SourceFile, TargetFile, ConversionType.ToString(), FinanceEntity.ToString(), 
              (OptVerbose) ? "  -> be verbose" + Environment.NewLine : "", 
              (HomebankSettingsFile != string.Empty) ? "  -> homebank settings file: " + HomebankSettingsFile + Environment.NewLine : "", 
              (PaymodePatternsFile != string.Empty) ? "  -> paymode patterns file: " + PaymodePatternsFile + Environment.NewLine : "", 
              (TargetAccountPattern != string.Empty) ? "  -> target account pattern: " + TargetAccountPattern + Environment.NewLine : "");
      }

      public string SourceFile { get { return _args["SOURCE"].ToString(); } }

      public string TargetFile { get { return _args["TARGET"].ToString(); } }

      public bool OptHelp { get { return _args["--help"].IsTrue; } }
      
      public bool OptVersion { get { return _args["--version"].IsTrue; } }

      public bool OptVerbose { get { return _args["--verbose"].IsTrue; } }

      public bool OptAppendDuplicates { get { return _args["--append-duplicates"].IsTrue; } }

      public string HomebankSettingsFile { get { return GetStringValue("--homebank-settings-file"); } }

      public string PaymodePatternsFile { get { return GetStringValue("--paymode-patterns-file"); } }

      public string TargetAccountPattern { get { return GetStringValue("--target-account-pattern"); } }
      
      public eFinanceEntityType FinanceEntity { get; private set; }

      public eConversionType ConversionType {
         get { return _conversionType; }
         set {
            if(_conversionType == value) return;
            _conversionType = value;
         }
      }
      private eConversionType _conversionType;

      private string GetStringValue(string key) {
          if(_args.ContainsKey(key) && _args[key] != null) return _args[key].ToString();
          return string.Empty;
      }

      private string GetConversionTypesDescription() {
         string str = "";
         DescriptionAttribute descr;

         foreach(var item in Util.GetEnumValues<eConversionType>()) {
            descr = item.GetAttribute<DescriptionAttribute>();
            str = String.Format("{0}      (*) {1} - {2}" + Environment.NewLine, str, item, descr.Description);
         }

         return str;
      }

      private string GetFinanceEntityTypesDescription() {
          string str = "";
          DescriptionAttribute descr;

          foreach(var item in Util.GetEnumValues<eFinanceEntityType>()) {
              descr = item.GetAttribute<DescriptionAttribute>();
              str = String.Format("{0}      (*) {1} - {2}" + Environment.NewLine, str, item, descr.Description);
          }

          return str;
      }
   }
}