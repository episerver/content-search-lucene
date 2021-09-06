using System;
using System.Collections.Generic;
using System.Linq;
//using EPiServer.Search.Configuration;
using EPiServer.Search.Filter;
using EPiServer.Search.Queries.Lucene;
using System.Globalization;
using EPiServer.ServiceLocation;

namespace EPiServer.Search
{
    /// <summary>
    /// Helper class to access search configuration
    /// </summary>
    public static class SearchSettings
    {
        private static Dictionary<string, SearchResultFilterProvider> _searchResultFilterProviders = new Dictionary<string,SearchResultFilterProvider>();
        private static SearchOptions _options;

        public static SearchOptions Options
        {
            get { return _options ?? new SearchOptions(); }
            set { _options = value; }
        }

        //[Obsolete("Add and retrieve indexing service references through the IndexingServiceReferences property on the SearchOptions class.", true)]
        //public static Dictionary<string, IndexingServiceReference> IndexingServiceReferences => throw new NotSupportedException();

        /// <summary>
        /// Gets all configured providers for filtering search results
        /// </summary>
        public static Dictionary<string, SearchResultFilterProvider> SearchResultFilterProviders
        {
            get
            {
                return _searchResultFilterProviders;
            }
        }

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
                case IndexAction.Add :
                    return "add";
                case IndexAction.Update :
                    return "update";
                case IndexAction.Remove :
                    return "remove";
                default :
                    return "";
            }
        }

        internal static void LoadSearchResultFilterProviders(SearchOptions options, IServiceProvider serviceProvider)
        {
            foreach (var factory in options.FilterProviders)
            {
                _searchResultFilterProviders.Add(factory.Key, factory.Value(serviceProvider));
            }
        }

        /// <summary>
        /// Occurs when all default configuration is loaded and may be overridden by code
        /// </summary>
        public static event EventHandler InitializationCompleted;

        internal static void OnInitializationCompleted()
        {
            InitializationCompleted?.Invoke(null, new EventArgs());
        }


        [Obsolete("Use IndexingServiceReferences instead", true)]
        public static Dictionary<string, object> IndexingServices { get; }

        [Obsolete("Use Options instead", true)]
        public static object Config { get; }
    }
}
