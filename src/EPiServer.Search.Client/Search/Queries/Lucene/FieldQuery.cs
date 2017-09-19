using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;

namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// Representing a field query for the Lucene Indexing Service with correct field name and Lucene syntax.
    /// </summary>
    public class FieldQuery : IQueryExpression
    {
        /// <summary>
        /// Search for the user entered query expression in the default field
        /// </summary>
        /// <param name="queryExpression">A user entered expression</param>
        public FieldQuery(string queryExpression)
        {
            Field = Field.Default;
            Expression = queryExpression.Trim();
        }

        /// <summary>
        /// Search for the user entered query expression in supplied <see cref="Field"/>
        /// </summary>
        /// <param name="queryExpression">A user entered expression</param>
        /// <param name="field">The <see cref="Field"/> to search in</param>
        public FieldQuery(string queryExpression, Field field)
        {
            Field = field;
            Expression = queryExpression.Trim();
        }

        /// <summary>
        /// gets and sets the field to search in
        /// </summary>
        public Field Field
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the user entered query expression for this <see cref="FieldQuery"/>
        /// </summary>
        public string Expression
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the text representation of this <see cref="FieldQuery"/> using configured field names
        /// </summary>
        /// <returns></returns>
        public virtual string GetQueryExpression()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(SearchSettings.GetFieldNameForField(Field));
            sb.Append(":(");
            sb.Append(LuceneHelpers.EscapeParenthesis(Expression));
            sb.Append(")");

            return sb.ToString();
        }

        /// <summary>
        /// Escapes a phrase and returns it in quoted form
        /// </summary>
        /// <param name="phrase">The phrase to escape and quote</param>
        /// <remarks>If the phrase is already quoted the contained string is still escaped</remarks>
        /// <returns>The quoted phrase</returns>
        protected static string GetSafeQuotedPhrase(string phrase)
        {
            if (phrase.StartsWith("\"", StringComparison.Ordinal))
                phrase = phrase.Substring(1);
            if (phrase.EndsWith("\"", StringComparison.Ordinal))
                phrase = phrase.Substring(0, phrase.Length - 1);

            return "\"" + LuceneHelpers.Escape(phrase) + "\"";
        }
    }
}
