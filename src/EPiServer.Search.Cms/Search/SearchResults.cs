using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EPiServer.Search
{
    public class SearchResults
    {
        private Collection<IndexResponseItem> _indexResponseItems = new Collection<IndexResponseItem>();

        public SearchResults()
        {
        }

        /// <summary>
        /// Gets the search results items list
        /// </summary>
        public Collection<IndexResponseItem> IndexResponseItems
        {
            get
            {
                return _indexResponseItems;
            }
        }

        /// <summary>
        /// Gets the total hits returned by the indexing service
        /// </summary>
        public int TotalHits
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the current version of the responding indexing service
        /// </summary>
        public string Version
        {
            get;
            internal set;
        }

    }
}
