using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace EPiServer.Search.Queries.Lucene
{
    public class VirtualPathQuery : IQueryExpression
    {
        private Collection<string> _virtualPathNodes = new Collection<string>();

        public VirtualPathQuery()
        {
        }

        /// <summary>
        /// Gets the list of nodes to build the virtual path. In order for a virtual path to match items in the indexing service, it needs to start from the root of the path.
        /// All spaces within a node will be removed.
        /// </summary>
        public Collection<string> VirtualPathNodes
        {
            get
            {
                return _virtualPathNodes;
            }
        }

        /// <summary> 
        /// Gets the query expression for this <see cref="VirtualPathQuery"/> in Lucene syntax
        /// All white spaces in node value will be removed
        /// </summary>
        /// <returns></returns>
        public virtual string GetQueryExpression()
        {
            StringBuilder expr = new StringBuilder();

            expr.Append(SearchSettings.Options.IndexingServiceFieldNameVirtualPath + ":(");

            foreach (string s in VirtualPathNodes)
            {
                expr.Append(LuceneHelpers.Escape(s.Replace(" ", "")));
                expr.Append("|");
            }

            if (expr.Length > 0)
                expr.Remove(expr.Length - 1, 1);

            expr.Append("*)");

            return expr.ToString();
        }
    }
}
