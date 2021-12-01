namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// Class with helper methods for constructing Lucene queries
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Lucene", Justification = "Known name")]
    public static class LuceneHelpers
    {
        /// <summary>
        /// Returns a string where those characters that QueryParser expects to be escaped are escaped by a preceding <code>\</code>.
        /// </summary>
        public static string Escape(string value)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                // These characters are part of the query syntax and must be escaped
                if (c == '\\' || c == '+' || c == '-' || c == '!' || c == '(' || c == ')' || c == ':' || c == '^' || c == '[' || c == ']' || c == '\"' || c == '{' || c == '}' || c == '~' || c == '*' || c == '?' || c == '|' || c == '&')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a string where parenthesis are escaped by a preceding <code>\</code>.
        /// </summary>
        public static string EscapeParenthesis(string value)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                // These characters are part of the query syntax and must be escaped
                if (c == '(' || c == ')')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
