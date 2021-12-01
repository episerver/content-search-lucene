using System;
using System.Collections.Generic;
using EPiServer.Search.Filter;
using EPiServer.Search.Queries.Lucene;

namespace EPiServer.Search
{
    /// <summary>
    /// Helper class to access search configuration
    /// </summary>
    public static class SearchSettings
    {
        private static SearchOptions _options;

        public static SearchOptions Options
        {
            get => _options ?? new SearchOptions();
            set => _options = value;
        }

        /// <summary>
        /// Gets all configured providers for filtering search results
        /// </summary>
        public static Dictionary<string, SearchResultFilterProvider> SearchResultFilterProviders { get; } = new Dictionary<string, SearchResultFilterProvider>();

        internal static string GetFieldNameForField(Field field)
        {
            switch (field)
            {
                case Field.Default:
                    return Options.IndexingServiceFieldNameDefault;
                case Field.Title:
                    return Options.IndexingServiceFieldNameTitle;
                case Field.DisplayText:
                    return Options.IndexingServiceFieldNameDisplayText;
                case Field.Authors:
                    return Options.IndexingServiceFieldNameAuthors;
                case Field.Id:
                    return Options.IndexingServiceFieldNameId;
                case Field.Created:
                    return Options.IndexingServiceFieldNameCreated;
                case Field.Modified:
                    return Options.IndexingServiceFieldNameModified;
                case Field.ItemType:
                    return Options.IndexingServiceFieldNameType;
                case Field.Culture:
                    return Options.IndexingServiceFieldNameCulture;
                case Field.ItemStatus:
                    return Options.IndexingServiceFieldNameItemStatus;
                default:
                    return Options.IndexingServiceFieldNameDefault;
            }
        }

        internal static string GetIndexActionName(IndexAction indexAction)
        {
            switch (indexAction)
            {
                case IndexAction.Add:
                    return "add";
                case IndexAction.Update:
                    return "update";
                case IndexAction.Remove:
                    return "remove";
                default:
                    return "";
            }
        }

        internal static void LoadSearchResultFilterProviders(SearchOptions options, IServiceProvider serviceProvider)
        {
            foreach (var factory in options.FilterProviders)
            {
                SearchResultFilterProviders.Add(factory.Key, factory.Value(serviceProvider));
            }
        }

        /// <summary>
        /// Occurs when all default configuration is loaded and may be overridden by code
        /// </summary>
        public static event EventHandler InitializationCompleted;

        internal static void OnInitializationCompleted() => InitializationCompleted?.Invoke(null, new EventArgs());


        [Obsolete("Use IndexingServiceReferences instead", true)]
        public static Dictionary<string, object> IndexingServices { get; }

        [Obsolete("Use Options instead", true)]
        public static object Config { get; }
    }
}
