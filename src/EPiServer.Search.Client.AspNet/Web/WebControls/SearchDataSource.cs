using EPiServer.Core;
using EPiServer.Core.Html;
using EPiServer.Filters;
using EPiServer.Search;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web.Hosting;
using EPiServer.Logging.Compatibility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using System.Web.Hosting;
using System.Web.UI;

namespace EPiServer.Web.WebControls
{
    /// <summary>
    /// Provides PageData data to DataBoundControl implementations through search based on various criteria.
    /// </summary>
    /// <example>
    /// <para>
    /// Refer to "Using Data Source Controls" under "Navigations and Listings" in the Developer Guide for more information and examples.
    /// </para>
    /// </example>
    [PersistChildren(false)]
    [ParseChildren(ChildrenAsProperties = true)]
    public class SearchDataSource : PageDataSource, IDataSource, IDataSourceMethods
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SearchDataSource));
        private const string DEFAULT_VIEW = "DefaultView";
        private const int DEFAULT_MAXALLOWHITS = 100;

        private List<PropertyCriteriaControl> _criteria = new List<PropertyCriteriaControl>();
        private string _languageBranches;
        private SearchIndexConfig _searchIndexConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchDataSource"/> class.
        /// </summary>
        public SearchDataSource()
            : this(ServiceLocation.ServiceLocator.Current.GetInstance<SearchIndexConfig>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchDataSource"/> class.
        /// </summary>
        /// <param name="searchIndexConfig">The search index config.</param>
        public SearchDataSource(SearchIndexConfig searchIndexConfig): base()
        {
            _searchIndexConfig = searchIndexConfig;
        }

        #region Accessors
        /// <summary>
        /// Search query string.
        /// </summary>
        /// <remarks>
        /// If SearchQuery is empty and the Criterias collection contains at 
        /// least one PropertyCriteria, the search will be performed using <see cref="EPiServer.DataFactory.FindPagesWithCriteria(PageReference, PropertyCriteriaCollection)"/>. 
        /// Default you will only get hits on pages where PageVisibleInMenu is true. If you want to get hits on pages where PageVisibleInMenu is false
        /// you need to set the property <see cref="EPiServer.Web.WebControls.PageDataSource.EnableVisibleInMenu"/> to false.
        /// </remarks>
        public string SearchQuery
        {
            get
            {
                if (SelectValues["SearchQuery"] != null)
                {
                    return (string)SelectValues["SearchQuery"];
                }
                return this.ViewState["SearchQuery"] != null ? (string)this.ViewState["SearchQuery"] : String.Empty;
            }
            set
            {
                this.ViewState["SearchQuery"] = value;
                RaiseChanged();
            }
        }

        /// <summary>
        /// By default no more than 100 hits can be returned for performance reasons
        /// </summary>
        public int MaxAllowHits
        {
            get
            {
                if (SelectValues["MaxAllowHits"] != null)
                {
                    return (int)SelectValues["MaxAllowHits"];
                }
                return this.ViewState["MaxAllowHits"] != null ? (int)this.ViewState["MaxAllowHits"] : DEFAULT_MAXALLOWHITS;
            }
            set
            {
                this.ViewState["MaxAllowHits"] = value;
                RaiseChanged();
            }
        }

        /// <summary>
        /// The search criterias that limit the search
        /// </summary>
        /// <remarks>
        /// The <see cref="PropertyCriteria"/> in the Criterias collection may be used in two different ways
        /// <list type="number">
        /// <item>
        /// If <see cref="SearchQuery"/> is not empty, the criteria in the collection will be converted to filters and 
        /// applied to the result of the text search. 
        /// </item>
        /// <item>
        /// If <see cref="SearchQuery"/> is empty, there will be no text search. Instead the search will be 
        /// resolved using <see cref="EPiServer.DataFactory.FindPagesWithCriteria(PageReference, PropertyCriteriaCollection)"/> based on the Criterias collection.
        /// </item>
        /// </list>
        /// </remarks>
        [PersistenceMode(PersistenceMode.InnerProperty), Category("Data"), DefaultValue((string)null), MergableProperty(false)]
        public List<PropertyCriteriaControl> Criteria
        {
            get { return _criteria; }
        }

        /// <summary>
        /// Gets or sets a comma separated list of the language branches to be searched in.
        /// </summary>
        /// <value>The language branches.</value>
        public string LanguageBranches
        {
            get { return _languageBranches; }
            set { _languageBranches = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="IPageCriteriaQueryService"/> that is used internally in this instance.
        /// </summary>
        [Browsable(false)]
        public Injected<IPageCriteriaQueryService> PageCriteriaSearchService { get; set; }

        internal Injected<SearchHandler> SearchHandler { get; set; }

        internal Injected<ContentSearchHandler> ContentSearchHandler { get; set; }

        internal Injected<IContentRepository> ContentRepository { get; set; }

        internal Injected<IAccessControlListQueryBuilder> QueryBuilder { get; set; }

        #endregion

        #region IDataSourceMethods Members

        public override System.Collections.IEnumerable Select(DataSourceSelectArguments arguments)
        {
            SearchResults searchResultsFromTextSearch;
            PageDataCollection pages = new PageDataCollection();
            PageReference pageLink = GetPageLink();

            // No page link set
            if (PageReference.IsNullOrEmpty(pageLink))
            {
                arguments.TotalRowCount = pages.Count;
                return pages;
            }


            // Property Search
            if (String.IsNullOrEmpty(SearchQuery))
            {
                if (Criteria.Count > 0)
                {
                    PropertyCriteriaCollection criteria = new PropertyCriteriaCollection();
                    foreach (PropertyCriteriaControl ctrl in Criteria)
                    {
                        criteria.Add(ctrl.InnerCriteria);
                    }

                    //Check if there is any value in LanguageBranches, and if so, make a search in each language branch
                    List<PageData> matches = new List<PageData>();
                    if (String.IsNullOrEmpty(LanguageBranches))
                    {
                        matches.AddRange(PageCriteriaSearchService.Service.FindPagesWithCriteria(pageLink, criteria));
                    }
                    else
                    {
                        foreach (string lang in LanguageBranches.Split(','))
                        {
                            matches.AddRange(PageCriteriaSearchService.Service.FindPagesWithCriteria(pageLink, criteria, lang));
                        }
                    }

                    foreach (PageData match in matches)
                    {
                        PageData page = match.CreateWritableClone();
                        page.Property.Add("PageRank", new PropertyNumber(1000));
                        page.MakeReadOnly();
                        pages.Add(page);
                    }

                    // A search has been made, so we raise the filter event.
                    PageLoader.ExecFilters(pages);
                }

                arguments.TotalRowCount = pages.Count;
                return pages;
            }

            log.Info("7.1.1 " + SearchQuery);

            if (!String.IsNullOrEmpty(SearchQuery))
            {
                FieldQuery query = new FieldQuery(AddTrailingWildcards(SearchQuery)); // Query for the entered search expression.

                GroupQuery groupQuery = new GroupQuery(LuceneOperator.AND);
                groupQuery.QueryExpressions.Add(query);

                var itemTypeGroup = new GroupQuery(LuceneOperator.OR);
                
                // Pages
                var pageGroup = new GroupQuery(LuceneOperator.AND);
                pageGroup.QueryExpressions.Add(new ContentQuery<PageData>());
                var pathQuery = new VirtualPathQuery();
                pathQuery.AddContentNodes(pageLink);
                if (!String.IsNullOrEmpty(LanguageBranches) && LanguageBranches.Trim().Length > 0)
                {
                    pageGroup.QueryExpressions.Add(GetLanguageBranchQuery());
                }
                pageGroup.QueryExpressions.Add(pathQuery);
                itemTypeGroup.QueryExpressions.Add(pageGroup);
                groupQuery.QueryExpressions.Add(itemTypeGroup);

                var aclQuery = new AccessControlListQuery();
                QueryBuilder.Service.AddUser(aclQuery, PrincipalInfo.CurrentPrincipal, Context);
                groupQuery.QueryExpressions.Add(aclQuery);

                searchResultsFromTextSearch = SearchHandler.Service.GetSearchResults(groupQuery, _searchIndexConfig.NamedIndexingService, _searchIndexConfig.NamedIndexes, 1, MaxAllowHits);

                if (searchResultsFromTextSearch != null && searchResultsFromTextSearch.IndexResponseItems != null)
                {
                    var iconPath = UriSupport.ResolveUrlBySettings("~/Util/images/Extensions/default.gif");

                    foreach (var result in searchResultsFromTextSearch.IndexResponseItems)
                    {
                        var page = ContentSearchHandler.Service.GetContent<PageData>(result);
                        if (page == null)
                            continue;

                        page = page.CreateWritableClone();
                        
                        page.Property.Add("IconPath", new PropertyString(iconPath));
                        page.Property.Add("PageRank", new PropertyNumber((int)(result.Score * 100)));

                        page.MakeReadOnly();
                        pages.Add(page);
                    }
                }
            }
            if (!String.IsNullOrEmpty(SearchQuery))
            {
                CreateFilters();
            }
            PageLoader.ExecFilters(pages);
            log.Info("7.1.1 Search query '" + AddTrailingWildcards(SearchQuery) + "' returned " + pages.Count + " matches");

            arguments.TotalRowCount = pages.Count;
            return pages;
        }

        private void CreateFilters()
        {
            if (PublishedStatus != PagePublishedStatus.Ignore)
            {
                FilterPublished published = new FilterPublished(PublishedStatus);
                PageLoader.Filter += published.Filter;
            }
            if (AccessLevel != AccessLevel.NoAccess)
            {
                FilterAccess access = new FilterAccess(AccessLevel);
                PageLoader.Filter += access.Filter;
            }
            if (FilterPagesWithoutTemplate)
            {
                FilterTemplate filterTemplate = new FilterTemplateAndFiles();
                PageLoader.Filter += filterTemplate.Filter;
            }

            foreach (PropertyCriteriaControl ctrl in Criteria)
            {
                PropertyCriteria criteria = ctrl.InnerCriteria;
                FilterCompareTo filter = new FilterCompareTo(criteria.Name, criteria.Value);
                filter.Condition = criteria.Condition;
                PageLoader.Filter += new FilterEventHandler(filter.Filter);
            }
        }

        private GroupQuery GetLanguageBranchQuery()
        {
            GroupQuery groupQueryLanguage = new GroupQuery(LuceneOperator.OR);
            if (!String.IsNullOrEmpty(LanguageBranches) && LanguageBranches.Trim().Length > 0)
            {
                string[] branches = LanguageBranches.Trim().Split(',', ' ', ';');

                foreach (String lang in branches)
                {
                    if (string.IsNullOrEmpty(lang)) { continue; }

                    FieldQuery queryLanguage = new FieldQuery(lang, Field.Culture);
                    groupQueryLanguage.QueryExpressions.Add(queryLanguage);
                }
            }
            return groupQueryLanguage;
        }

        public override int Delete(System.Collections.IDictionary values)
        {
            throw new NotSupportedException("Delete is not supported.");
        }

        #endregion

        public void RaiseChanged()
        {
            this.OnDataSourceChanged(EventArgs.Empty);
        }

        private class FilterTemplateAndFiles : FilterTemplate
        {
            public override bool ShouldFilter(PageData page)
            {
                if (page["IsFile"] != null)
                {
                    return false;
                }
                return base.ShouldFilter(page);
            }
        }

        public virtual String AddTrailingWildcards(String query)
        {
            return !query.Contains(" ")  && query.LastIndexOf('*') < 0 ? query + "*" : query;
        }
    }
}
