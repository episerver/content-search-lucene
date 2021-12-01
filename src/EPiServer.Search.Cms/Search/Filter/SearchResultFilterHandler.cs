namespace EPiServer.Search.Filter
{
    /// <summary>
    /// Class responsible consulting the configured providers and make decisions whether the items should be included in the search result
    /// </summary>
    public static class SearchResultFilterHandler
    {
        /// <summary>
        /// Iterates all configured providers until it finds a provider that handles this type and returns whether the item should be included or excluded from search results.
        /// If no configured provider exists for the item type, it returns the configured DefaultInclude settings.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>True if the item should be included and False if the item should be excluded</returns>
        public static bool Include(IndexResponseItem item)
        {
            // Iterate all configured providers
            foreach (var provider in SearchSettings.SearchResultFilterProviders.Values)
            {
                var filter = provider.Filter(item);
                if (filter == SearchResultFilter.Include)
                {
                    return true;
                }
                else if (filter == SearchResultFilter.Exclude)
                {
                    return false;
                }

                // if SearchResultFilter.NotHandled, try next provider
            }

            return SearchSettings.Options.SearchResultFilterDefaultInclude;
        }

    }
}
