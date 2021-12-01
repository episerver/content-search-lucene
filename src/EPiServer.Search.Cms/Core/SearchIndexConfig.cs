using System.Collections.ObjectModel;
using EPiServer.ServiceLocation;

namespace EPiServer.Core
{

    /// <summary>
    /// holds configuration about NamedIndex and NamedIndexingService
    /// </summary>
    [ServiceConfiguration(typeof(SearchIndexConfig), Lifecycle = ServiceInstanceScope.Singleton)]
    public class SearchIndexConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchIndexConfig"/> class.
        /// </summary>
        public SearchIndexConfig()
        {
        }

        /// <summary>
        /// Gets or sets the index of the named.
        /// </summary>
        public virtual Collection<string> NamedIndexes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the index of the CMS named.
        /// </summary>
        /// <value>
        /// The index of the CMS named.
        /// </value>
        public virtual string CMSNamedIndex
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the named indexing service.
        /// </summary>
        public virtual string NamedIndexingService
        {
            get;
            set;
        }

    }
}
