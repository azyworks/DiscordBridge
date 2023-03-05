using System.Text;
using System.Text.RegularExpressions;

namespace DiscordBridgeBot.Core.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveHtmlTags(this string text)
        {
            var openTagIndexes = Regex.Matches(text, "<").Cast<Match>().Select(m => m.Index).ToList();
            var closeTagIndexes = Regex.Matches(text, ">").Cast<Match>().Select(m => m.Index).ToList();

            if (closeTagIndexes.Count > 0)
            {
                var sb = new StringBuilder();
                var previousIndex = 0;

                foreach (int closeTagIndex in closeTagIndexes)
                {
                    var openTagsSubset = openTagIndexes.Where(x => x >= previousIndex && x < closeTagIndex);

                    if (openTagsSubset.Count() > 0 && closeTagIndex - openTagsSubset.Max() > 1)
                    {
                        sb.Append(text.Substring(previousIndex, openTagsSubset.Max() - previousIndex));
                    }
                    else
                    {
                        sb.Append(text.Substring(previousIndex, closeTagIndex - previousIndex + 1));
                    }

                    previousIndex = closeTagIndex + 1;
                }

                if (closeTagIndexes.Max() < text.Length)
                {
                    sb.Append(text.Substring(closeTagIndexes.Max() + 1));
                }

                return sb.ToString();
            }
            else
            {
                return text;
            }
        }
    }
}
