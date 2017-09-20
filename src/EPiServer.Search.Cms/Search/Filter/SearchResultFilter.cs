using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EPiServer.Search.Filter
{
    /// <summary>
    /// An enum returned by the SearchResultFilterProviders telling whether the item should be included, excluded ocr if the provider does noy handle the item type
    /// </summary>
    public enum SearchResultFilter
    {
        /// <summary>
        /// Include item in search result
        /// </summary>
        Include = 1,
        /// <summary>
        /// Exclude item in search result
        /// </summary>
        Exclude = 2,
        /// <summary>
        /// The item type is not handled by this provider
        /// </summary>
        NotHandled = 3
    }
}
