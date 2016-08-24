using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FinTransConverterLib.Helpers {
   public static class Util {
      public static IEnumerable<EnumType> GetEnumValues<EnumType>() {
         return Enum.GetValues(typeof(EnumType)).Cast<EnumType>();
      }
   }

   public static class EnumExtensions {
      /// <summary>
      /// Gets an attribute on an enum field value.
      /// </summary>
      /// <typeparam name="TAttr">The type of the attribute to retrieve.</typeparam>
      /// <param name="enumVal">The enum value.</param>
      /// <returns>The attribute of type TAttr that exists on the enum value.</returns>
      public static TAttr GetAttribute<TAttr>(this Enum enumVal) where TAttr : Attribute {
         return enumVal.GetType()
            .GetTypeInfo()
            .GetMember(enumVal.ToString())
            .FirstOrDefault()
            .GetCustomAttributes(false)
            .OfType<TAttr>()
            .FirstOrDefault();
      }
   }

    public static class StringExtensions {
        public static String Indent(this String str, string indent) {
            string target = "";
            var splitString = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach(var substring in splitString) {
                target = String.Format("{0}{1}{2}" + Environment.NewLine, target, indent, substring);
            }

            return target;
        }
    }
}