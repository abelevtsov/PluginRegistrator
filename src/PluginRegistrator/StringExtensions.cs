using System.Text.RegularExpressions;

namespace PluginRegistrator
{
    public static class StringExtensions
    {
        public static string SplitCamelCase(this string s)
        {
            return Regex.Replace(s, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
        }
    }
}
