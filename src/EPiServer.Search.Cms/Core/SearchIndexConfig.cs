using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Collections.ObjectModel;
using EPiServer.ServiceLocation;

namespace EPiServer.Core
{

    /// <summary>
    /// holds configuration about NamedIndex and NamedIndexingService
    /// </summary>
   [ServiceConfiguration(typeof(SearchIndexConfig), Lifecycle= ServiceInstanceScope.Singleton)]
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
        public virtual Collection<String> NamedIndexes
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
        public virtual String CMSNamedIndex
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the named indexing service.
        /// </summary>
        public virtual String NamedIndexingService
        {
            get;
            set;
        }

   }
}
