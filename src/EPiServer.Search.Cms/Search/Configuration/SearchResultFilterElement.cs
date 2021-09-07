
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;

namespace EPiServer.Search.Configuration
{
    public class SearchResultFilterElement
    {
        /// <summary>
        /// Gets and sets whether the default behaviour for filtering should be to include results when no provider is configured for the type. Default = false.
        /// </summary>
        public bool SearchResultFilterDefaultInclude { get; set; }

        /// <summary>
        /// Gets and sets list of provider
        /// </summary>
        public List<ProviderElement> Providers { get; set; }
    }
}
