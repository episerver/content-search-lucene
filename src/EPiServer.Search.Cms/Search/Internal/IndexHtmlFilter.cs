using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using EPiServer.HtmlParsing;

namespace EPiServer.Search.Internal
{
    /// <summary>
    /// Parses out indexeable text from HTML
    /// </summary>
    public class IndexHtmlFilter
    {
        /// <summary>
        /// A list of HTML 5 block tags that semantically for text can be translated to a line break
        /// </summary>
        private static readonly HashSet<string> _textSeparators = new HashSet<string>
            {
                "br", "p", "pre", "section", "td", "div", "h1", "h2", "h3", "h4", "h5", "h6", "li", "hr", "blockquote", "address", "nav", "article", "main", "aside", "header", "footer", "figcaption"
            };

        public string StripHtml(string input)
        {
            var newLineWritten = true;

            using (var output = new StringWriter(CultureInfo.InvariantCulture))
            {
                foreach (var fragment in new HtmlStreamReader(input, ParserOptions.HtmlOptions | ParserOptions.TagNamesToLower))
                {
                    switch (fragment.FragmentType)
                    {
                        case HtmlFragmentType.Element:
                            if (!newLineWritten && _textSeparators.Contains(fragment.Name))
                            {
                                output.WriteLine();
                                newLineWritten = true;
                            }
                            break;

                        case HtmlFragmentType.Text:
                            if (!string.IsNullOrEmpty(fragment.Value))
                            {
                                if (fragment.Value == Environment.NewLine && newLineWritten)
                                {
                                    continue;
                                }
                                fragment.ToWriter(output);
                                newLineWritten = fragment.Value.EndsWith(Environment.NewLine, StringComparison.Ordinal);
                            }
                            break;
                    }
                }

                return WebUtility.HtmlDecode(output.ToString());
            }
        }
    }
}
