using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;

namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// Representing a field query for the Lucene Indexing Service with correct field name and Lucene syntax, escaping any special characters present.
    /// </summary>
    public class EscapedFieldQuery : FieldQuery
    {
        /// <summary>
        /// Search for the user entered query expression in the default field
        /// </summary>
        /// <param name="queryExpression">A user entered expression</param>
        public EscapedFieldQuery(string queryExpression) : base(queryExpression)
        {
        }

        /// <summary>
        /// Search for the user entered query expression in supplied <see cref="Field"/>
        /// </summary>
        /// <param name="queryExpression">A user entered expression</param>
        /// <param name="field">The <see cref="Field"/> to search in</param>
        public EscapedFieldQuery(string queryExpression, Field field) : base(queryExpression, field)
        {
        }

        /// <summary>
        /// Gets the text representation of this <see cref="EscapedFieldQuery"/> using configured field names
        /// </summary>
        /// <returns></returns>
        public override string GetQueryExpression()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(SearchSettings.GetFieldNameForField(Field));
            sb.Append(":(");
            sb.Append(LuceneHelpers.Escape(Expression));
            sb.Append(")");

            return sb.ToString();
        }
    }
}
