using System.Text.RegularExpressions;

namespace PluginRegistrator.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex CamelCaseSplitRegex = new Regex("([A-Z])", RegexOptions.Compiled);

        public static string SplitCamelCase(this string input, string replacement = " $1") => CamelCaseSplitRegex.Replace(input, replacement).Trim();
    }
}
