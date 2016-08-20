using System;

namespace FinTransConverterLib.Csv {
   public class CsvField {
      
   }

   public class CsvSettings {
      public String Delimiter { get; set; }
      //public char String
   }

   public enum eCsvFieldType {
      String, 
      Number, 
      DateTime
   }
}