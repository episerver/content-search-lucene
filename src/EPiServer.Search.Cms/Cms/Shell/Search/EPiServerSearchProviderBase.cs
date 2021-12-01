using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer.Authorization;
using EPiServer.Cms.Shell.Search.Internal;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Localization;
using EPiServer.Search;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Newtonsoft.Json.Linq;

namespace EPiServer.Cms.Shell.Search
{
    /// <summary>
    /// Base search provider for searing for content in EPiServer CMS
    /// </summary>
    /// <typeparam name="TContentData">The type of the content data.</typeparam>
    /// <typeparam name="TContentType">The type of the content type.</typeparam>
    public abstract class EPiServerSearchProviderBase<TContentData, TContentType> : ContentSearchProviderBase<TContentData, TContentType>
        where TContentData : IContentData
        where TContentType : ContentType
    {
        private readonly IContentRepository _contentRepository;
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly SearchHandler _searchHandler;
        private readonly ContentSearchHandler _contentSearchHandler;
        private readonly SearchIndexConfig _searchIndexConfig;
        private readonly UIDescriptorRegistry _uiDescriptorRegistry;

        private bool? _isSearchActive;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentSearchProviderBase&lt;TContentData, TContentType&gt;" /> class.
        /// </summary>
        protected EPiServerSearchProviderBase(LocalizationService localizationService, ISiteDefinitionResolver siteDefinitionResolver, IContentTypeRepository<TContentType> contentTypeRepository, EditUrlResolver editUrlResolver,
            ServiceAccessor<SiteDefinition> currentSiteDefinition, IContentRepository contentRepository, ILanguageBranchRepository languageBranchRepository, SearchHandler searchHandler, ContentSearchHandler contentSearchHandler,
            SearchIndexConfig searchIndexConfig, UIDescriptorRegistry uiDescriptorRegistry, IContentLanguageAccessor languageResolver, UrlResolver urlResolver, TemplateResolver templateResolver)
            : base(localizationService, siteDefinitionResolver, contentTypeRepository, editUrlResolver, currentSiteDefinition, languageResolver, urlResolver, templateResolver, uiDescriptorRegistry)
        {
            Validator.ThrowIfNull("contentRepository", contentRepository);

            _contentRepository = contentRepository;
            _languageBranchRepository = languageBranchRepository;
            _searchHandler = searchHandler;
            _contentSearchHandler = contentSearchHandler;
            _searchIndexConfig = searchIndexConfig;
            _uiDescriptorRegistry = uiDescriptorRegistry;

            HasAdminAccess = () => PrincipalInfo.CurrentPrincipal.IsInRole(Roles.Administrators);
        }

        /// <summary>
        /// Delegate to make PrincipalInfo testable
        /// </summary>
        public Func<bool> HasAdminAccess { get; set; }

        /// <summary>
        /// If search is active, for testability only.
        /// </summary>
        public virtual bool IsSearchActive
        {
            get => !_isSearchActive.HasValue ? ServiceLocation.ServiceLocator.Current.GetInstance<SearchOptions>().Active : _isSearchActive.Value;
            set => _isSearchActive = value;
        }

        /// <summary>
        /// Searches the specified query.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// A list of search results, containing links to the specific content in edit mode.
        public override IEnumerable<SearchResult> Search(Query query)
        {
            Validator.ThrowIfNull("query", query);

            if (string.IsNullOrEmpty(query.SearchQuery) || !IsSearchActive)
            {
                return Enumerable.Empty<SearchResult>();
            }

            var results = new List<SearchResult>();

            var editAccessCultures = GetEditAccessCultures();

            //Have they entered an ID?
            if (ContentReference.TryParse(query.SearchQuery, out var contentLink) && !ContentReference.IsNullOrEmpty(contentLink) && contentLink != ContentReference.SelfReference)
            {
                //If found add it to the hits
                if (_contentRepository.TryGet<TContentData>(contentLink, out var content))
                {
                    var securable = content as ISecurable;
                    var icontent = content as IContent;
                    var searchInWasteBasket = query.SearchRoots.Any(p => p.Equals(ContentReference.WasteBasket.ID.ToString()));

                    if ((searchInWasteBasket || icontent == null || !icontent.IsDeleted) && (securable == null || securable.GetSecurityDescriptor().HasAccess(PrincipalInfo.CurrentPrincipal, AccessLevel.Read)))
                    {
                        var localizable = content as ILocalizable;

                        if (localizable != null)
                        {
                            if (editAccessCultures.Contains(localizable.Language))
                            {
                                results.Add(CreateSearchResult(content));
                            }
                        }
                        else
                        {
                            results.Add(CreateSearchResult(content));
                        }
                    }
                }
            }

            var groupQuery = new GroupQuery(LuceneOperator.AND);
            var fieldQueries = new GroupQuery(LuceneOperator.OR);
            fieldQueries.QueryExpressions.Add(new FieldQuery(AddTrailingWildcards(query.SearchQuery)));
            fieldQueries.QueryExpressions.Add(new TermBoostQuery(AddTrailingWildcards(query.SearchQuery), Field.Title, 5));
            groupQuery.QueryExpressions.Add(fieldQueries);
            if (query.SearchRoots.Any())
            {
                var pathGroupQuery = new GroupQuery(LuceneOperator.OR);
                foreach (var root in query.SearchRoots)
                {
                    if (ContentReference.TryParse(root, out var searchRoot))
                    {
                        var pathQuery = new VirtualPathQuery();
                        foreach (var node in _contentSearchHandler.GetVirtualPathNodes(searchRoot))
                        {
                            pathQuery.VirtualPathNodes.Add(node);
                        }

                        pathGroupQuery.QueryExpressions.Add(pathQuery);
                    }
                }
                if (pathGroupQuery.QueryExpressions.Any())
                {
                    groupQuery.QueryExpressions.Add(pathGroupQuery);
                }
            }

            Func<string, IEnumerable<Type>> getContentTypesFromQuery = parameter =>
            {
                if (query.Parameters.ContainsKey(parameter))
                {
                    var array = query.Parameters[parameter] as JArray;
                    if (array != null)
                    {
                        return array.Values<string>().SelectMany(GetContentTypes);
                    }
                }
                return Enumerable.Empty<Type>();
            };

            var allowedTypes = getContentTypesFromQuery("allowedTypes");
            var allowedTypesGroup = new GroupQuery(LuceneOperator.OR);
            foreach (var allowedType in allowedTypes)
            {
                allowedTypesGroup.QueryExpressions.Add(new ContentTypeQuery(allowedType));
            }

            var restrictedTypes = getContentTypesFromQuery("restrictedTypes");
            var restrictedTypesGroup = new GroupQuery(LuceneOperator.OR);
            foreach (var restrictedType in restrictedTypes)
            {
                restrictedTypesGroup.QueryExpressions.Add(new ContentTypeQuery(restrictedType));
            }


            if (!allowedTypes.Any())
            {
                allowedTypesGroup.QueryExpressions.Add(new ContentQuery<TContentData>());
            }

            GroupQuery contentTypeGroup;
            if (restrictedTypes.Any())
            {
                contentTypeGroup = new GroupQuery(LuceneOperator.NOT);
                contentTypeGroup.QueryExpressions.Add(allowedTypesGroup);
                contentTypeGroup.QueryExpressions.Add(restrictedTypesGroup);
            }
            else
            {
                contentTypeGroup = new GroupQuery(LuceneOperator.OR);
                contentTypeGroup.QueryExpressions.Add(allowedTypesGroup);
            }

            groupQuery.QueryExpressions.Add(contentTypeGroup);

            // Add ACL check
            if (!HasAdminAccess())
            {
                var aclQuery = new AccessControlListQuery();
                aclQuery.AddAclForUser(new AccessControlList());
                groupQuery.QueryExpressions.Add(aclQuery);
            }

            var cultureQuery = new GroupQuery(LuceneOperator.OR);
            AddLanguageFilter(query, editAccessCultures, cultureQuery);
            if (cultureQuery.QueryExpressions.Count > 0)
            {
                groupQuery.QueryExpressions.Add(cultureQuery);
            }

            var searchResultsFromIndex = _searchHandler.GetSearchResults(groupQuery, _searchIndexConfig.NamedIndexingService, _searchIndexConfig.NamedIndexes, 1, query.MaxResults * 5);

            if (searchResultsFromIndex != null)
            {
                // Don't included deleted items in the results if the filterOnDeleted flag is set to true.
                var filterOnDeleted = query.Parameters != null && query.Parameters.ContainsKey("filterOnDeleted") && bool.Parse(query.Parameters["filterOnDeleted"].ToString());
                var contentResults = FilterResults(query, from result in searchResultsFromIndex.IndexResponseItems
                                                          let content = _contentSearchHandler.GetContent<IContent>(result, query.FilterOnCulture)
                                                          where content != null && content.QueryDistinctAccess(AccessLevel.Read) && (!filterOnDeleted || !content.IsDeleted)
                                                          select (TContentData)content);

                results.AddRange(contentResults.Take(query.MaxResults - results.Count).Select(content => CreateSearchResult(content)));
            }

            return results;
        }

        /// <summary>
        /// Get the content types for the allowedType string
        /// </summary>
        /// <param name="allowedType">The type to get the content type for</param>
        /// <returns></returns>
        protected virtual IEnumerable<Type> GetContentTypes(string allowedType)
        {
            var uiDescriptor = _uiDescriptorRegistry.UIDescriptors.FirstOrDefault(d => d.TypeIdentifier.Equals(allowedType, StringComparison.OrdinalIgnoreCase));
            if (uiDescriptor == null)
            {
                return Enumerable.Empty<Type>();
            }

            return ContentTypeRepository
                .List()
                .Where(c => uiDescriptor.ForType.IsAssignableFrom(c.ModelType))
                .Select(c => c.ModelType);
        }

        /// <summary>
        /// Override this method to add any extra filtering needed on the results
        /// </summary>
        /// <param name="query">The query</param>
        /// <param name="result">The search results</param>
        protected virtual IEnumerable<TContentData> FilterResults(Query query, IEnumerable<TContentData> result) => result;

        /// <summary>
        /// Adds a language filter to the query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="editAccessCultures">The edit access cultures.</param>
        /// <param name="cultureQuery">The culture query.</param>
        protected virtual void AddLanguageFilter(Query query, List<CultureInfo> editAccessCultures, GroupQuery cultureQuery)
        {
            if (query.FilterOnCulture)
            {
                //If we should filter on culture, only get hits on current language.
                cultureQuery.QueryExpressions.Add(new FieldQuery(LanguageResolver.Language.Name, Field.Culture));
            }
            else
            {
                // Add cultures that the user has edit access to
                foreach (var culture in editAccessCultures)
                {
                    cultureQuery.QueryExpressions.Add(new FieldQuery(culture.Name, Field.Culture));
                }
            }
        }

        public virtual string AddTrailingWildcards(string query) => query.IndexOfAny(new char[] { '*', ' ' }) < 0 ? query + "*" : query;

        #region Helper Methods

        private List<CultureInfo> GetEditAccessCultures()
        {
            return _languageBranchRepository.ListEnabled()
                .Where(languageBranch => languageBranch.ACL.QueryDistinctAccess(AccessLevel.Edit))
                .Select(lb => lb.Culture).ToList();
        }

        #endregion
    }
}
