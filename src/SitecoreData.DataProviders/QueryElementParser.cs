using System;
using System.Text.RegularExpressions;

namespace SitecoreData.DataProviders
{
    public class QueryElementParser
    {
        private const int NotFound = -1;

        public static string GetName(string input)
        {
            
            var firstIndex = input.IndexOf('\'');
            var lastIndex = input.LastIndexOf('\'');
            int indexOfDoubleQuote = input.IndexOf('\"');
            if (DoubleQuotePrecedesSingleQuote(indexOfDoubleQuote, firstIndex))
            {
                firstIndex = indexOfDoubleQuote;
                lastIndex = input.LastIndexOf('\"');
            }
            if (firstIndex == NotFound)
            {
                return String.Empty;
            }
            int length = (lastIndex - 1) - firstIndex;
            
            return input.Substring(firstIndex + 1, length);
        }

        private static bool DoubleQuotePrecedesSingleQuote(int indexOfDoubleQuote, int indexOfSingleQuote)
        {
            if (indexOfDoubleQuote == NotFound)
            {
                return false;
            }
            if (indexOfSingleQuote == NotFound && indexOfDoubleQuote != NotFound)
            {
                return true;
            }
            return indexOfDoubleQuote < indexOfSingleQuote;
        }

        public static Guid GetGuidFromPredicate(string predicate)
        {
            var match= Regex.Match(predicate,
                                   @"\{?[a-fA-F\d]{8}-([a-fA-F\d]{4}-){3}[a-fA-F\d]{12}\}?");
            if (match.Success)
            {
                return new Guid(match.Value);
            }
            return Guid.Empty;
        }

        public static string GetPredicate(string input)
        {
            string predicate = input.Substring(input.IndexOf("[") + 1);
            predicate = predicate.Substring(0, predicate.IndexOf("]"));
            return predicate;
        }
    }
}