using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using EPiServer.Core;
using EPiServer.Core.Internal;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Blobs;
using EPiServer.Logging.Compatibility;
using EPiServer.Search.Data;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.SpecializedProperties;
using EPiServer.Web;
using Microsoft.Extensions.Options;

namespace EPiServer.Search.Internal
{
    [ServiceConfiguration(ServiceType = typeof(ContentSearchHandler))]
    [ServiceConfiguration(ServiceType = typeof(IReIndexable))]
    public class ContentSearchHandlerImplementation : ContentSearchHandler
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ContentSearchHandler));
        private const string IgnoreItemSearchId = "<IgnoreItemId>";

        public const string ItemTypeSeparator = " ";
        public static readonly string BaseItemType = GetItemTypeSection<IContent>();
        public const char SearchItemIdSeparator = '|';

        private readonly IContentTypeRepository _contentTypeRepository;
        private readonly IContentRepository _contentRepository;
        private readonly SearchHandler _searchHandler;
        private readonly Collection<string> _namedIndexes = null;
        private readonly SearchIndexConfig _searchIndexConfig;
        private readonly IPrincipalAccessor _principalAccessor;
        private bool? _searchActive;
        private readonly IAccessControlListQueryBuilder _queryBuilder;
        private readonly SearchOptions _options;
        private readonly RequestQueueHandler _requestQueueHandler;
        private readonly RequestHandler _requestHandler;

        public ContentSearchHandlerImplementation(SearchHandler searchHandler,
            IContentRepository contentRepository,
            IContentTypeRepository contentTypeRepository,
            SearchIndexConfig searchIndexConfig,
            IPrincipalAccessor principalAccessor,
            IAccessControlListQueryBuilder queryBuilder,
            IOptions<SearchOptions> options,
            RequestQueueHandler requestQueueHandler,
            RequestHandler requestHandler)
        {
            Validator.ThrowIfNull("searchHandler", searchHandler);
            Validator.ThrowIfNull("contentRepository", contentRepository);
            Validator.ThrowIfNull("contentTypeRepository", contentTypeRepository);

            _searchHandler = searchHandler;
            _contentRepository = contentRepository;
            _contentTypeRepository = contentTypeRepository;
            _searchIndexConfig = searchIndexConfig;
            _principalAccessor = principalAccessor;
            _queryBuilder = queryBuilder;
            if (NamedIndex != null)
            {
                _namedIndexes = new Collection<string>
                {
                    NamedIndex
                };
            }
            _options = options.Value;
            _requestQueueHandler = requestQueueHandler;
            _requestHandler = requestHandler;
        }


        /// <summary>
        /// Adds all published content to the index by calling this.UpdateIndex for each item under RootPage
        /// </summary>
        public override void IndexPublishedContent()
        {
            if (!ServiceActive)
            {
                return;
            }

            var reader = new SlimContentReader(_contentRepository, ContentReference.RootPage, c => { var s = c as ISearchable; return s == null ? true : s.AllowReIndexChildren; });

            var requestQueueItems = new List<IndexRequestQueueItem>();

            while (reader.Next())
            {
                if (!reader.Current.ContentLink.CompareToIgnoreWorkID(ContentReference.RootPage))
                {
                    var versionStatus = reader.Current as IVersionable;

                    // If the content supports version status, we check that we don't index any non published content.
                    if (versionStatus == null || (versionStatus.Status == VersionStatus.Published))
                    {
                        var indexRequestItem = GetIndexRequestItem(reader.Current);
                        if (indexRequestItem != null)
                        {
                            requestQueueItems.Add(indexRequestItem);
                        }
                    }
                }
            }

            foreach (var serviceReference in _options.IndexingServiceReferences)
            {
                try
                {
                    while (true)
                    {
                        if (requestQueueItems.Count == 0)
                        {
                            break;
                        }

                        var contentRequestToIndex = requestQueueItems
                            .OrderBy(x => x.IndexItemId)
                            .Take(_options.DequeuePageSize);

                        requestQueueItems = requestQueueItems
                            .OrderBy(x => x.IndexItemId)
                            .Skip(_options.DequeuePageSize).ToList();

                        var feed = _requestQueueHandler.GetUnprocessedFeed(contentRequestToIndex);

                        var success = _requestHandler.SendRequest(feed, serviceReference.Name);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"RequestQueue failed to retrieve unprocessed queue items.", ex);
                    break;
                }
            }
        }

        /// <summary>
        /// Updates the search index representation of the provided content item.
        /// </summary>
        /// <param name="contentItem">The content item that should be re-indexed.</param>
        /// <remarks>
        /// Note that only the exact language version that is provided is updated. If you want to 
        /// update all language versions of a page, use alternative method overload.
        /// </remarks>
        private IndexRequestQueueItem GetIndexRequestItem(IContent contentItem)
        {
            var item = GetItemToIndex(contentItem);

            if (item == null)
            {
                return null;
            }

            var namedIndexingService = string.IsNullOrEmpty(NamedIndexingService) ?
                _options.DefaultIndexingServiceName :
                NamedIndexingService;

            return new IndexRequestQueueItem()
            {
                IndexItemId = item.Id,
                NamedIndex = item.NamedIndex,
                NamedIndexingService = namedIndexingService,
                FeedItemJson = item.ToFeedItemJson(_options),
                Timestamp = DateTime.Now
            };
        }

        /// <summary>
        /// Updates the search index representation of the provided content item.
        /// </summary>
        /// <param name="contentItem">The content item that should be re-indexed.</param>
        /// <remarks>
        /// Note that only the exact language version that is provided is updated. If you want to 
        /// update all language versions of a page, use alternative method overload.
        /// </remarks>
        public override void UpdateItem(IContent contentItem)
        {
            var item = GetItemToIndex(contentItem);

            if (item == null)
            {
                return;
            }

            _searchHandler.UpdateIndex(item, NamedIndexingService);
        }

        /// <summary>
        /// Updates the search index for the provided content item and it's descendants 
        /// with a new virtual path location.
        /// </summary>
        /// <param name="contentLink">The reference to the content item that is the root item that should get a new virtual path in the search index.</param>
        /// <remarks>
        /// The content of the provided item will also be included as a part of the update.
        /// </remarks>
        public override void MoveItem(ContentReference contentLink)
        {
            if (!ServiceActive)
            {
                return;
            }

            if (ContentReference.IsNullOrEmpty(contentLink))
            {
                return;
            }

            if (!_contentRepository.TryGet<IContent>(contentLink, CultureInfo.InvariantCulture, out var contentItem))
            {
                // Move between providers means delete from source provider
                return;
            }

            var searchId = GetSearchId(contentItem);

            var item = new IndexRequestItem(searchId, IndexAction.Update)
            {
                AutoUpdateVirtualPath = true
            };

            // We still need to include all info since this is updated server side.
            ConvertContentToIndexItem(contentItem, item);

            _searchHandler.UpdateIndex(item, NamedIndexingService);
        }

        /// <summary>
        /// Removes all content items located at or under the provided virtual node from the search index.
        /// This will include all language versions as well.
        /// </summary>
        /// <param name="virtualPathNodes">The collection of virtual path nodes used to determine what items to remove.</param>
        public override void RemoveItemsByVirtualPath(ICollection<string> virtualPathNodes)
        {
            Validator.ThrowIfNull("virtualPathNodes", virtualPathNodes);

            if (!ServiceActive || virtualPathNodes.Count == 0)
            {
                return;
            }

            var item = new IndexRequestItem(IgnoreItemSearchId, IndexAction.Remove);

            // By setting the Virtual path and AutoUpdateVirtualPath we will delete all items with the same or descendant virtual path
            foreach (var node in virtualPathNodes)
            {
                item.VirtualPathNodes.Add(node);
            }
            item.AutoUpdateVirtualPath = true;
            item.NamedIndex = NamedIndex;
            _searchHandler.UpdateIndex(item, NamedIndexingService);
        }

        /// <summary>
        /// Removes a language branch of a content item from the search index.
        /// </summary>
        /// <param name="contentItem">The content item that should be removed from the search index.</param>
        public override void RemoveLanguageBranch(IContent contentItem)
        {
            Validator.ThrowIfNull("contentItem", contentItem);

            if (!ServiceActive)
            {
                return;
            }

            var searchId = GetSearchId(contentItem);
            var item = new IndexRequestItem(searchId, IndexAction.Remove)
            {
                NamedIndex = NamedIndex
            };
            _searchHandler.UpdateIndex(item, NamedIndexingService);
        }

        /// <summary>
        /// Gets a collection of virtual path nodes for a content item to use in the search index.
        /// </summary>
        /// <param name="contentLink">The content link.</param>
        /// <returns>A collection of virtual path nodes.</returns>
        public override ICollection<string> GetVirtualPathNodes(ContentReference contentLink)
        {
            Validator.ThrowIfNull("contentLink", contentLink);

            var nodes = new Collection<string>();

            foreach (var ancestor in _contentRepository.GetAncestors(contentLink).Reverse())
            {
                nodes.Add(ancestor.ContentGuid.ToString());
            }

            // Add the item itself
            var item = _contentRepository.Get<IContent>(contentLink);
            nodes.Add(item.ContentGuid.ToString());

            return nodes;
        }

        /// <summary>
        /// Gets the item type representation for the provided content item type that is used in the search index.
        /// </summary>
        /// <param name="contentType">Type of the content.</param>
        /// <returns>
        /// A string representing the full ItemType.
        /// </returns>
        /// <remarks>
        /// This string will be made up by the base type of the provided type together with a generic name
        /// idicating that it is a content item.
        /// </remarks>
        public override string GetItemType(Type contentType)
        {
            Validator.ThrowIfNull("contentType", contentType);

            var itemType = new StringBuilder(GetItemTypeSection(contentType));

            if (contentType.IsClass)
            {
                while (contentType.BaseType != typeof(object))
                {
                    contentType = contentType.BaseType;
                    itemType.Append(ItemTypeSeparator);
                    itemType.Append(GetItemTypeSection(contentType));
                }
            }

            // TODO: Add interfaces on the type as well? (Third party would be good, every interface on PageData might be excessive)

            itemType.Append(ItemTypeSeparator);
            itemType.Append(BaseItemType);

            return itemType.ToString();
        }

        /// <summary>
        /// Converts the <paramref name="indexItem"/> to the correct <see cref="IContent"/> instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="indexItem">The index item.</param>
        /// <param name="filterOnCulture">if set to <c>true</c> filter on culture.</param>
        /// <returns>
        ///   <c>null</c> if <paramref name="indexItem"/> is not valid; otherwise a <see cref="PageData"/> instance.
        /// </returns>
        /// <remarks>
        ///   <para>The Id of <paramref name="indexItem"/> must start with a guid that matches a page.</para>
        ///   <para>if <paramref name="filterOnCulture"/> is <c>false</c> it will use the Culture of <paramref name="indexItem"/> 
        ///     to specify of what culture the returned <see cref="IContent"/> should be. If <paramref name="filterOnCulture"/>
        ///     is <c>false</c> it will only get content in the current culture.</para>
        /// </remarks>
        public override T GetContent<T>(IndexItemBase indexItem, bool filterOnCulture)
        {
            if (indexItem == null || string.IsNullOrEmpty(indexItem.Id))
            {
                return default(T);
            }

            var guidString = indexItem.Id.Split(SearchItemIdSeparator).FirstOrDefault();
            if (Guid.TryParse(guidString, out var contentGuid))
            {
                var selector = filterOnCulture ? new LoaderOptions() { LanguageLoaderOption.Fallback() } : GetLoaderOptions(indexItem.Culture);

                try
                {
                    return _contentRepository.Get<T>(contentGuid, selector);
                }
                catch (ContentNotFoundException ex)
                {
                    _log.Warn(string.Format(CultureInfo.InvariantCulture, "Search index returned an item with GUID {0:B}, that no longer exists in the content repository.", contentGuid), ex);
                }
            }

            return default(T);
        }

        /// <summary>
        /// Gets the search result for the specified query.
        /// </summary>
        /// <typeparam name="T">The type of content that should be returned.</typeparam>
        /// <param name="searchQuery">The search query.</param>
        /// <param name="root">The root for the search.</param>
        /// <param name="page">The page index of the result. Used to handle paging. Most be larger than 0.</param>
        /// <param name="pageSize">Number of items per page. Used to handle paging.</param>
        /// <param name="filterOnAccess">if set to <c>true</c>, items that the user doesn't have read access to will be removed.</param>
        /// <returns>
        /// The search result matching the search query.
        /// </returns>
        public override SearchResults GetSearchResults<T>(string searchQuery, ContentReference root, int page, int pageSize, bool filterOnAccess)
        {
            if (!ServiceActive)
            {
                return null;
            }

            var groupQuery = new GroupQuery(LuceneOperator.AND);

            groupQuery.QueryExpressions.Add(new ContentQuery<T>());
            groupQuery.QueryExpressions.Add(new FieldQuery(searchQuery));

            if (!ContentReference.IsNullOrEmpty(root))
            {
                var pathQuery = new VirtualPathQuery();
                foreach (var node in GetVirtualPathNodes(root))
                {
                    pathQuery.VirtualPathNodes.Add(node);
                }
                groupQuery.QueryExpressions.Add(pathQuery);
            }

            if (filterOnAccess)
            {
                var aclQuery = new AccessControlListQuery();
                _queryBuilder.AddUser(aclQuery, _principalAccessor.Principal, null);

                groupQuery.QueryExpressions.Add(aclQuery);
            }
            return _searchHandler.GetSearchResults(groupQuery, NamedIndexingService, _namedIndexes, page, pageSize);
        }

        public override bool ServiceActive
        {
            get => _searchActive.HasValue ? (bool)_searchActive : SearchSettings.Options.Active;
            set => _searchActive = value;
        }

        private static string GetSearchId(IContent content)
        {
            CultureInfo language = null;
            var languageData = content as ILocale;
            if (languageData != null)
            {
                language = languageData.Language;
            }

            return CreateSearchId(content.ContentGuid, language);
        }

        private static string CreateSearchId(Guid contentGuid, CultureInfo language) => string.Concat(contentGuid, SearchItemIdSeparator, GetCultureIdentifier(language));

        private LoaderOptions GetLoaderOptions(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return new LoaderOptions() { LanguageLoaderOption.FallbackWithMaster() };
            }

            var culture = languageCode == InvariantCultureIndexedName ?
                CultureInfo.InvariantCulture :
                CultureInfo.GetCultureInfo(languageCode);

            return new LoaderOptions() { LanguageLoaderOption.Specific(culture) };
        }

        private void ConvertContentToIndexItem(IContent content, IndexRequestItem item)
        {
            var securable = content as IContentSecurable;
            var categorizable = content as ICategorizable;

            AddUriToIndexItem(content, item);
            AddMetaDataToIndexItem(content, item);
            AddSearchablePropertiesToIndexItem(content, item);
            AddBinaryStorableToIndexItem(content, item);
            if (securable != null)
            {
                var descriptor = securable.GetContentSecurityDescriptor();
                if (descriptor != null && descriptor.Entries != null)
                {
                    AddReadAccessToIndexItem(descriptor.Entries, item);
                }
            }
            else
            {
                // If the item doesn't support access rights, add the everyone role
                item.AccessControlList.Add(string.Format(CultureInfo.InvariantCulture, "G:{0}", EveryoneRole.RoleName));
            }

            if (categorizable != null)
            {
                AddCategoriesToIndexItem(categorizable.Category, item);
            }

            AddVirtualPathToIndexItem(content.ContentLink, item);
            AddItemStatusToIndexItem(item);
            AddExpirationToIndexItem(content, item);
            item.NamedIndex = NamedIndex;
        }

        private static void AddBinaryStorableToIndexItem(IContent content, IndexRequestItem item)
        {
            //We only support indexing local files when using EPiServer Search
            var binaryStorable = content as IBinaryStorable;
            if (binaryStorable != null && binaryStorable.BinaryData is FileBlob)
            {
                item.DataUri = new Uri(((FileBlob)binaryStorable.BinaryData).FilePath);
            }
        }

        /// <summary>
        /// Adds the passed <see cref="IContent"/> permanent link with the epslanguage query parameter set to the page LanguageID to the passed <see cref="IndexRequestItem"/>
        /// </summary>
        /// <param name="content">The <see cref="IContent"/> to get the <see cref="Uri"/> from</param>
        /// <param name="item">The <see cref="IndexRequestItem"/> to add the <see cref="Uri"/> to</param>
        private static void AddUriToIndexItem(IContent content, IndexRequestItem item)
        {
            var permaPath = PermanentLinkUtility.GetPermanentLinkVirtualPath(content.ContentGuid, ".aspx");
            var locale = content as ILocale;
            if (locale != null && locale.Language != null)
            {
                permaPath = UriUtil.AddLanguageSelection(permaPath, locale.Language.Name);
            }
            item.Uri = new Url(permaPath).Uri;
        }

        /// <summary>
        /// Adds the passed <see cref="IContent"/>'s PageName, Created, Changed and LanguageID property values to the passed <see cref="IndexRequestItem"/>'s Title, Created, Modified, Culture and ItemType properties
        /// </summary>
        /// <param name="content">The <see cref="IContent"/></param>
        /// <param name="item">The <see cref="IndexRequestItem"/></param>
        private void AddMetaDataToIndexItem(IContent content, IndexRequestItem item)
        {
            var languageData = content as ILocale;
            var changeTrackable = content as IChangeTrackable;

            item.Title = content.Name;
            item.Created = changeTrackable != null ? new DateTimeOffset(changeTrackable.Created) : DateTimeOffset.MinValue;
            item.Modified = changeTrackable != null ? new DateTimeOffset(changeTrackable.Changed) : DateTimeOffset.MinValue;
            item.Culture = languageData != null ? GetCultureIdentifier(languageData.Language) : string.Empty;
            item.ItemType = GetItemType(content.GetOriginalType());
            item.Authors.Add(changeTrackable != null ? changeTrackable.CreatedBy : string.Empty);
        }

        /// <summary>
        /// Adds searchable <see cref="IContent"/> properties to the passed <see cref="IndexRequestItem"/>' DisplayText property
        /// </summary>
        /// <param name="content">The <see cref="IContent"/></param>
        /// <param name="item">The <see cref="IndexRequestItem"/></param>
        private void AddSearchablePropertiesToIndexItem(IContent content, IndexRequestItem item) => item.DisplayText = string.Join(Environment.NewLine, GetSearchablePropertyValues(content, content.ContentTypeID).ToArray());

        private IEnumerable<string> GetSearchablePropertyValues(IContentData contentData, int contentTypeID) => GetSearchablePropertyValues(contentData, _contentTypeRepository.Load(contentTypeID));

        private IEnumerable<string> GetSearchablePropertyValues(IContentData contentData, Type modelType) => GetSearchablePropertyValues(contentData, _contentTypeRepository.Load(modelType));

        private IEnumerable<string> GetSearchablePropertyValues(IContentData contentData, ContentType contentType)
        {
            if (contentType != null)
            {
                foreach (var propertyDefinition in contentType.PropertyDefinitions.Where(d => d.Searchable || typeof(IPropertyBlock).IsAssignableFrom(d.Type.DefinitionType)))
                {
                    var property = contentData.Property[propertyDefinition.Name];

                    var blockProperty = property as IPropertyBlock;
                    if (blockProperty != null)
                    {
                        foreach (var blockPropertyValue in GetSearchablePropertyValues(blockProperty.Block, blockProperty.BlockType))
                        {
                            yield return blockPropertyValue;
                        }
                    }
                    else
                    {
                        yield return property.ToWebString();
                    }
                }
            }
        }

        private static void AddCategoriesToIndexItem(CategoryList categories, IndexRequestItem item)
        {
            if (categories == null || categories.Count == 0)
            {
                return;
            }

            foreach (var categoryId in categories)
            {
                item.Categories.Add(categoryId.ToString());
            }
        }

        private void AddVirtualPathToIndexItem(ContentReference contentLink, IndexRequestItem item)
        {
            if (!ContentReference.IsNullOrEmpty(contentLink))
            {
                foreach (var node in GetVirtualPathNodes(contentLink))
                {
                    item.VirtualPathNodes.Add(node);
                }
            }
        }

        /// <summary>
        /// Adds a category with the status approved
        /// </summary>
        /// <param name="item">The <see cref="IndexRequestItem"/> that should have the status category set</param>
        private static void AddItemStatusToIndexItem(IndexRequestItem item) => item.ItemStatus = ItemStatus.Approved;

        /// <summary>
        /// Adds the passed <see cref="IContent"/> StopPublish as expiration date to the passed <see cref="IndexRequestItem"/>
        /// </summary>
        /// <param name="content">The <see cref="IContent"/> for which to use the StopPublish time</param>
        /// <param name="item">The <see cref="IndexRequestItem"/> that should have the expiration date set</param>
        private static void AddExpirationToIndexItem(IContent content, IndexRequestItem item)
        {
            var versionable = content as IVersionable;
            if (versionable != null && versionable.StopPublish != DateTime.MaxValue)
            {
                item.PublicationEnd = versionable.StopPublish;
            }
        }

        /// <summary>
        /// Adds AccessControlList from the <see cref="PageData"/> to the passed <see cref="IndexRequestItem"/>'s AccessControlList
        /// </summary>
        /// <param name="acl">The <see cref="AccessControlList"/> containing the AccessControlList</param>
        /// <param name="item">The <see cref="IndexRequestItem"/> that should have the AccessControlList set</param>
        internal static void AddReadAccessToIndexItem(IEnumerable<AccessControlEntry> acl, IndexRequestItem item)
        {
            foreach (var kvp in acl)
            {
                if ((kvp.Access & EPiServer.Security.AccessLevel.Read) == EPiServer.Security.AccessLevel.Read)
                {
                    item.AccessControlList.Add(string.Format("{0}:{1}", kvp.EntityType == EPiServer.Security.SecurityEntityType.User ? "U" : "G", kvp.Name));
                }
            }
        }

        /// <summary>
        /// Gets the index of the named.
        /// </summary>
        public override string NamedIndex => _searchIndexConfig != null ? _searchIndexConfig.CMSNamedIndex : null;

        /// <summary>
        /// Gets the named indexing service.
        /// </summary>
        public override string NamedIndexingService => _searchIndexConfig != null ? _searchIndexConfig.NamedIndexingService : null;

        private IndexRequestItem GetItemToIndex(IContent contentItem)
        {
            Validator.ThrowIfNull("contentItem", contentItem);

            if (!ServiceActive)
            {
                return null;
            }

            // Don't add item if ISearchable.IsSearchable return false
            var searchable = contentItem as ISearchable;
            if (searchable != null && !searchable.IsSearchable)
            {
                return null;
            }

            var searchId = GetSearchId(contentItem);

            var item = new IndexRequestItem(searchId, IndexAction.Update);

            ConvertContentToIndexItem(contentItem, item);

            return item;
        }
    }
}
