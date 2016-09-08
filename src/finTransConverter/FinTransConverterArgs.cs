using System;
using System.Collections.Generic;
using System.ComponentModel;
using DocoptNet;
using FinTransConverterLib.Helpers;
using FinTransConverterLib.FinanceEntities;
using FinTransConverterLib.FinanceEntities.Homebank;

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
            "   -v, --version  Output version information and exit." + Environment.NewLine + 
            "   --homebankSettingsFile=HOMEBANKSETTINGS  Only valid when target conversion is a Hombank format." + Environment.NewLine + 
            "      Use this option to import a Homebank settings file (*.xhb). This will be parsed and used to" + Environment.NewLine + 
            "      refine the conversion, e.g. for automatic transaction assignment." + Environment.NewLine + 
            "   --paymodePatternsFile=PAYMODEPATTERNS  Only valid when target conversion is a Homebank format." + Environment.NewLine + 
            "      Use this option to import a paymode patterns file (*.xpmp). This will be parsed and used to" + Environment.NewLine + 
            "      refine the conversion." + Environment.NewLine;
         _args = new Docopt().Apply(USAGE, argv, help, version, optionsFirst, exit);
         ConversionType = GetConversionType(_args["CONVERSIONTYPE"].ToString());
      }

      public IDictionary<string, DocoptNet.ValueObject> Args { get { return _args; } }

      public string SourceFile { get { return _args["SOURCE"].ToString(); } }

      public string TargetFile { get { return _args["TARGET"].ToString(); } }

      public bool OptHelp { get { return _args["--help"].IsTrue; } }
      
      public bool OptVersion { get { return _args["--version"].IsTrue; } }

      public string HomebankSettingsFile { get { return _args["--homebankSettingsFile"].ToString(); } }

      public string PaymodePatternsFile { get { return _args["--paymodePatternsFile"].ToString(); } }

      public eConversionType ConversionType {
         get { return _conversionType; }
         set {
            if(_conversionType == value) return;
            _conversionType = value;
         }
      }
      private eConversionType _conversionType;

      private string GetConversionTypesDescription() {
         string str = "";
         DescriptionAttribute descr;

         foreach(var item in Util.GetEnumValues<eConversionType>()) {
            descr = item.GetAttribute<DescriptionAttribute>();
            str = String.Format("      (*) {0}{1} - {2}" + Environment.NewLine, str, item, descr.Description);
         }

         return str;
      }

      private eConversionType GetConversionType(string convTypeString) {
         foreach(var enumItem in Util.GetEnumValues<eConversionType>()) {
            if(convTypeString.Equals(enumItem.ToString())) return enumItem;
         }

         throw new DocoptInputErrorException("Invalid conversion type.");
      }
   }
}