using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace EPiServer.Search.Queries.Lucene
{
    public abstract class CollectionQueryBase : IQueryExpression
    {
        Collection<string> _items = new Collection<string>();

        protected CollectionQueryBase(string itemFieldName, LuceneOperator innerOperator)
        {
            InnerOperator = innerOperator;
            IndexFieldName = itemFieldName;
        }

        /// <summary>
        /// The operator to use between the collection Items
        /// </summary>
        public LuceneOperator InnerOperator
        {
            get;
            private set;
        }

        /// <summary>
        /// The name of the corresponding document field in the indexing service 
        /// </summary>
        public string IndexFieldName
        {
            get;
            private set;
        }

        /// <summary>
        /// Items to add to this collection query
        /// </summary>
        public Collection<string> Items
        {
            get
            {
                return _items;
            }
        }

        /// <summary> 
        /// Gets the query expression for this <see cref="AccessControlListQuery"/> in Lucene syntax
        /// </summary>
        /// <returns></returns>
        public virtual string GetQueryExpression()
        {
            StringBuilder expr = new StringBuilder();
            Collection<string> nonDupeList = RemoveDuplicates(Items);
            int i = 0;

            foreach (string racl in nonDupeList)
            {
                i++;

                expr.Append(IndexFieldName + ":(");

                if (i < nonDupeList.Count)
                {
                    expr.Append(LuceneHelpers.Escape(racl));
                    expr.Append(") ");
                    expr.Append(Enum.GetName(typeof(LuceneOperator), InnerOperator));
                    expr.Append(" ");
                }
                else
                {
                    expr.Append(LuceneHelpers.Escape(racl));
                    expr.Append(")");
                }

            }

            return expr.ToString();
        }

        private static Collection<string> RemoveDuplicates(Collection<string> inputList)
        {
            Dictionary<string, int> uniqueStore = new Dictionary<string, int>();
            Collection<string> finalList = new Collection<string>();

            foreach (string currValue in inputList)
            {
                if (!uniqueStore.ContainsKey(currValue))
                {
                    uniqueStore.Add(currValue, 0);
                    finalList.Add(currValue);
                }
            }
            return finalList;
        }
    }
}
