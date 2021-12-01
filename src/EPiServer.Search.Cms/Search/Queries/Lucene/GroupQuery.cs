using System;
using System.Collections.ObjectModel;
using System.Text;

namespace EPiServer.Search.Queries.Lucene
{
    public class GroupQuery : IQueryExpression
    {
        private readonly Collection<IQueryExpression> _queries = new Collection<IQueryExpression>();

        /// <summary>
        /// The <see cref="LuceneOperator"/> to use between the queries within the group
        /// </summary>
        /// <param name="innerOperator"></param>
        public GroupQuery(LuceneOperator innerOperator)
        {
            InnerOperator = innerOperator;
        }

        public LuceneOperator InnerOperator
        {
            get;
            set;
        }

        public Collection<IQueryExpression> QueryExpressions => _queries;

        /// <summary>
        /// Gets the text representation for this <see cref="GroupQuery"/>
        /// </summary>
        /// <returns></returns>
        public string GetQueryExpression()
        {
            var text = new StringBuilder();
            var i = 0;
            var itemsCount = QueryExpressions.Count;

            foreach (var searchExpression in QueryExpressions)
            {
                i++;

                if (itemsCount > 1)
                {
                    text.Append("(");
                }

                text.Append(searchExpression.GetQueryExpression());

                if (i < QueryExpressions.Count)
                {
                    //Add inner operator if this is not the last grouped item
                    text.Append(") " + Enum.GetName(typeof(LuceneOperator), InnerOperator) + " ");
                }
                else if (itemsCount > 1)
                {
                    text.Append(")");
                }
            }

            return text.ToString();
        }
    }
}
