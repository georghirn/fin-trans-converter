using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HtmlAgilityPack;

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
      public static String Indent(this String str, String indent) {
         string target = "";
         var splitString = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
         
         foreach(var substring in splitString) {
               if(target == "") {
                  target = String.Format("{0}{1}{2}", target, indent, substring);
               } else {
                  target = String.Format("{0}" + Environment.NewLine + "{1}{2}", target, indent, substring);
               }
         }

         return target;
      }

      public static enumType ToEnum<enumType>(this String str) {
         foreach(var enumItem in Util.GetEnumValues<enumType>()) {
            if(str.Equals(enumItem.ToString())) return enumItem;
         }

         throw new KeyNotFoundException("The string does not match with an enum value.");
      }

      public static String RemoveWhitespaces(this String str) {
          return new String(str
              .ToCharArray()
              .Where(c => Char.IsWhiteSpace(c) == false)
              .ToArray()
          );
      }
   }

    public static class DateTimeExtensions {
        public const double JulianUnixZero = 719163;
        public static readonly DateTime UnixZeroDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime JulianToDateTime(this DateTime d, UInt32 julianDate) {
            var sourceKind = d.Kind;
            double unixTime = (julianDate - JulianUnixZero) * 86400;
            d = UnixZeroDateTime;
            d = d.AddSeconds(unixTime);
            return (sourceKind != DateTimeKind.Utc) ? d.ToLocalTime() : d;
        }

        public static UInt32 ToJulianDate(this DateTime d) {
            TimeSpan diff = ((d.Kind != DateTimeKind.Utc) ? d.ToUniversalTime() : d) - UnixZeroDateTime;
            double unixTime = Math.Floor(diff.TotalSeconds);
            UInt32 julianDate = (UInt32)(Math.Floor(unixTime / 86400) + JulianUnixZero);
            return julianDate;
        }
    }

    public static class HtmlNodeCollectionExtensions {
        public static List<HtmlNode> Where(this HtmlNodeCollection source, Func<HtmlNode, bool> predicate) {
            var list = new List<HtmlNode>();
            foreach(var node in source) if(predicate(node)) list.Add(node);
            return list;
        }
    }
}