using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EPiServer.ServiceLocation;

namespace EPiServer.Search
{
    /// <summary>
    /// Makes the search handler of the public API.
    /// </summary>
    public static class ServiceLocationHelperExtensions
    {
        /// <summary>
        /// Resolves the <see cref="SearchHandler"/>.
        /// </summary>
        /// <param name="helper">The service helper.</param>
        /// <returns>
        /// The currently registered <see cref="SearchHandler"/>.
        /// </returns>
        public static SearchHandler SearchHandler(this ServiceLocationHelper helper)
        {
            return helper.Advanced.GetInstance<SearchHandler>();
        }
    }
}
