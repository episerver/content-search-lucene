using System;
using System.Collections.Generic;
using EPiServer.Framework;
//using EPiServer.Search.Configuration;
using EPiServer.Search.Filter;
using EPiServer.ServiceLocation;

namespace EPiServer.Search
{
    /// <summary>
    /// Defines settings for search 
    /// </summary>
    [Options]
    public class SearchOptions
    {
        /// <summary>
        /// Gets or sets if the search service is active
        /// </summary>
        /// <remarks>Default value is false</remarks>
        public bool Active { get; set; } = false;

        /// <summary>
        /// Specifes the name of the CMS index for the application
        /// </summary>
        public string CMSNamedIndex { get; set; }

        /// <summary>
        /// Specifes the name of the indexing server for the CMS application
        /// </summary>
        public string CMSNamedIndexingService { get; set; }

        /// <summary>
        /// Gets and sets a timer interval in seconds when the requests queue to indexing service should be dequeued
        /// </summary>
        /// <remarks>Default value is 30 seconds</remarks>
        public int QueueFlushInterval { get; set; } = 30;

        /// <summary>
        /// Gets and sets the maximum number of hits returned by the indexing service
        /// </summary>
        /// <remarks>Default value is 500</remarks>
        public int MaxHitsFromIndexingService { get; set; } = 500;

        /// <summary>
        /// Gets and sets whether to send paging parameters passed to GetSearchResults (page and pagesize) to service. If set to false page=1 and pagesize=[MaxHits] will sent.
        /// This should be set to false if any SearchResultFilter is plugged in.
        /// </summary>
        /// <remarks>Default value is true</remarks>
        public bool UseIndexingServicePaging { get; set; } = true;

        /// <summary>
        /// Gets the name of the Dynamic Data Store used to store items to be indexed
        /// </summary>
        /// <remarks>Default value is 'IndexRequestQueueDataStore'</remarks>
        public string DynamicDataStoreName { get; set; } = "IndexRequestQueueDataStore";

        /// <summary>
        /// Gets and sets the uri template for update reqeuest to the indexing service
        /// </summary>
        /// <remarks>Default value is '/update/?accesskey={accesskey}'</remarks>
        public string UpdateUriTemplate { get; set; } = "/update/?accesskey={accesskey}";

        /// <summary>
        /// Gets and sets the uri template for search requests to the indexing service. The Http method is always GET
        /// Required replaceables: "{q}", {namedIndexes}
        /// </summary>
        /// <remarks>Default value is '/search/?q={q}&amp;namedindexes={namedindexes}&amp;offset={offset}&amp;limit={limit}&amp;format=xml&amp;accesskey={accesskey}'</remarks>
        public string SearchUriTemplate { get; set; } = "/search/?q={q}&namedindexes={namedindexes}&offset={offset}&limit={limit}&format=xml&accesskey={accesskey}";

        /// <summary>
        /// Gets and sets the uri template for reset index request to the indexing service
        /// </summary>
        /// <remarks>Default value is '/reset/?namedindex={namedindex}&amp;accesskey={accesskey}'</remarks>
        public string ResetUriTemplate { get; set; } = "/reset/?namedindex={namedindex}&accesskey={accesskey}";

        /// <summary>
        /// Gets and sets the Http method for Reset index requests to the indexing service
        /// </summary>
        /// <remarks>Default value is 'POST'</remarks>
        public string ResetHttpMethod { get; set; } = "POST";

        /// <summary>
        /// Gets and sets the uri template for getting all named indexes
        /// </summary>
        /// <remarks>Default value is '/namedindexes/?accesskey={accesskey}'</remarks>
        public string NamedIndexesUriTemplate { get; set; } = "/namedindexes/?accesskey={accesskey}";

        /// <summary>
        /// Gets and sets the namespace for XmlQualifiedName
        /// </summary>
        /// <remarks>Default value is 'EPiServer.Search.IndexingService'</remarks>
        public string XmlQualifiedNamespace { get; set; } = "EPiServer.Search.IndexingService";

        /// <summary>
        /// Gets and sets the name for the syndication feed attribute extension TotalHits
        /// </summary>
        /// <remarks>Default value is 'TotalHits'</remarks>
        public string SyndicationFeedAttributeNameTotalHits { get; set; } = "TotalHits";

        /// <summary>
        /// Gets and sets the name for the syndication feed attribute extension Version
        /// </summary>
        /// <remarks>Default value is 'Version'</remarks>
        public string SyndicationFeedAttributeNameVersion { get; set; } = "Version";

        /// <summary>
        /// Gets and sets the name for the syndication item attribute extension Culture
        /// </summary>
        /// <remarks>Default value is 'Culture'</remarks>
        public string SyndicationItemAttributeNameCulture { get; set; } = "Culture";

        /// <summary>
        /// Gets and sets the name for the syndication item attribute extension Type
        /// </summary>
        /// <remarks>Default value is 'Type'</remarks>
        public string SyndicationItemAttributeNameType { get; set; } = "Type";

        /// <summary>
        /// gets and sets the name for the syndication item attribute extension ReferenceId
        /// </summary>
        /// <remarks>Default value is 'ReferenceId'</remarks>
        public string SyndicationItemAttributeNameReferenceId { get; set; } = "ReferenceId";

        /// <summary>
        /// gets and sets the name for the syndication item attribute extension ItemStatus
        /// </summary>
        /// <remarks> Default value is 'ItemStatus'</remarks>
        public string SyndicationItemAttributeNameItemStatus { get; set; } = "ItemStatus";

        /// <summary>
        /// gets and sets the name for the syndication item element extension ACL
        /// </summary>
        /// <remarks>Default value is 'ACL'</remarks>
        public string SyndicationItemElementNameAcl { get; set; } = "ACL";

        /// <summary>
        /// gets and sets the name for the syndication item attribute extension VirtualPath
        /// </summary>
        /// <remarks>Default value is 'VirtualPath'</remarks>
        public string SyndicationItemElementNameVirtualPath { get; set; } = "VirtualPath";


        /// <summary>
        /// Gets and sets the name for the syndication item element extension Metadata
        /// </summary>
        /// <remarks>Default value is 'Metadata'</remarks>
        public string SyndicationItemElementNameMetadata { get; set; } = "Metadata";

        /// <summary>
        /// Gets and sets the name for the syndication item attribute extension BoostFactor
        /// </summary>
        /// <remarks>Default value is 'BoostFactor'</remarks>
        public string SyndicationItemAttributeNameBoostFactor { get; set; } = "BoostFactor";

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension NamedIndex
        /// </summary>
        /// <remarks>Default value is 'NamedIndex'</remarks>
        public string SyndicationItemAttributeNameNamedIndex { get; set; } = "NamedIndex";

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension IndexAction
        /// </summary>
        /// <remarks>Default value is 'IndexAction'</remarks>
        public string SyndicationItemAttributeNameIndexAction = "IndexAction";

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension AutoUpdateVirtualPath
        /// </summary>
        /// <remarks>Default value is 'AutoUpdateVirtualPath'</remarks>
        public string SyndicationItemAttributeNameAutoUpdateVirtualPath { get; set; } = "AutoUpdateVirtualPath";

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension Version
        /// </summary>
        /// <remarks>Default value is 'Version'</remarks>
        public string SyndicationItemAttributeNameVersion { get; set; } = "Version";

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension CallbackUri
        /// </summary>
        /// <remarks>Default value is 'DataUri'</remarks>
        public string SyndicationItemAttributeNameDataUri { get; set; } = "DataUri";

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension Score
        /// </summary>
        /// <remarks>Default value is 'Score'</remarks>
        public string SyndicationItemAttributeNameScore { get; set; } = "Score";

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension PublicationEnd
        /// </summary>
        /// <remarks>Default value is 'PublicationEnd'</remarks>
        public string SyndicationItemAttributeNamePublicationEnd { get; set; } = "PublicationEnd";

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension PublicationStart
        /// </summary>
        /// <remarks>Default value is 'PublicationStart'</remarks>
        public string SyndicationItemAttributeNamePublicationStart { get; set; } = "PublicationStart";

        /// <summary>
        /// Gets and sets the name for the indexing service field name ID
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_ID'</remarks>
        public string IndexingServiceFieldNameId { get; set; } = "EPISERVER_SEARCH_ID";

        /// <summary>
        /// Gets and sets the name for the indexing service field name Default
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_DEFAULT'</remarks>
        public string IndexingServiceFieldNameDefault { get; set; } = "EPISERVER_SEARCH_DEFAULT";

        /// <summary>
        /// Gets and sets the name for the indexing service field name Title
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_TITLE'</remarks>
        public string IndexingServiceFieldNameTitle { get; set; } = "EPISERVER_SEARCH_TITLE";

        /// <summary>
        /// Gets and sets the name for the indexing service field name DisplayText
        /// </summary>
        /// <remarks>
        /// Default value is 'EPISERVER_SEARCH_DISPLAYTEXT'</remarks>
        public string IndexingServiceFieldNameDisplayText { get; set; } = "EPISERVER_SEARCH_DISPLAYTEXT";

        /// <summary>
        /// Gets and sets the name for the indexing service field name Authors
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_AUTHORS'</remarks>
        public string IndexingServiceFieldNameAuthors { get; set; } = "EPISERVER_SEARCH_AUTHORS";

        /// <summary>
        /// Gets and sets the name for the indexing service field name Created
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_CREATED'</remarks> 
        public string IndexingServiceFieldNameCreated { get; set; } = "EPISERVER_SEARCH_CREATED";

        /// <summary>
        /// Gets and sets the name for the indexing service field name Modified
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_MODIFIED'</remarks>
        public string IndexingServiceFieldNameModified { get; set; } = "EPISERVER_SEARCH_MODIFIED";

        /// <summary>
        /// Gets and sets the name for the indexing service field name Categories
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_CATEGORIES'</remarks>
        public string IndexingServiceFieldNameCategories { get; set; } = "EPISERVER_SEARCH_CATEGORIES";

        /// <summary>
        /// Gets and sets the name for the indexing service field name Read Access Control List
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_ACL'</remarks>
        public string IndexingServiceFieldNameAcl { get; set; } = "EPISERVER_SEARCH_ACL";

        /// <summary>
        /// Gets and sets the name for the indexing service field name VirtualPath
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_VIRTUALPATH'</remarks>
        public string IndexingServiceFieldNameVirtualPath { get; set; } = "EPISERVER_SEARCH_VIRTUALPATH";

        /// <summary>
        /// Gets and sets the name for the indexing service field name Type
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_TYPE'</remarks>
        public string IndexingServiceFieldNameType { get; set; } = "EPISERVER_SEARCH_TYPE";

        /// <summary>
        /// Gets and sets the name for the indexing service field name Culture
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_CULTURE'</remarks>
        public string IndexingServiceFieldNameCulture { get; set; } = "EPISERVER_SEARCH_CULTURE";

        /// <summary>
        /// Gets and sets the name for the indexing service field name ItemStatus
        /// </summary>
        /// <remarks>Default value is 'EPISERVER_SEARCH_ITEMSTATUS'</remarks>
        public string IndexingServiceFieldNameItemStatus { get; set; } = "EPISERVER_SEARCH_ITEMSTATUS";

        /// <summary>
        /// Gets and sets and sets the page size to use when dequeueing the request queue
        /// </summary>
        /// <remarks>Default value is 50</remarks>
        public int DequeuePageSize { get; set; } = 50;

        /// <summary>
        /// Gets and sets and sets whether to automatically strip HTML from the IndexItem Title before sending it to indexing service
        /// </summary>
        /// <remarks>Default value is true</remarks>
        public bool HtmlStripTitle { get; set; } = true;

        /// <summary>
        /// Gets and sets and sets whether to automatically strip HTML from the IndexItem DisplayText before sending it to indexing service
        /// </summary>
        /// <remarks>Default value is true</remarks>
        public bool HtmlStripDisplayText { get; set; } = true;

        /// <summary>
        /// Gets and sets and sets whether to automatically strip HTML from the IndexItem Metadata before sending it to indexing service
        /// </summary>
        /// <remarks>Default value is true</remarks>
        public bool HtmlStripMetadata { get; set; } = true;

        /// <summary>
        /// Factories for search filter providers
        /// </summary>
        public Dictionary<string, Func<IServiceProvider, SearchResultFilterProvider>> FilterProviders { get; } = new Dictionary<string, Func<IServiceProvider, SearchResultFilterProvider>>();

        // TO BE UPDATED
        /// <summary>
        /// Contains a list of references for indexing services.
        /// </summary>
        //public List<IndexingServiceReference> IndexingServiceReferences { get; } = new List<IndexingServiceReference>();

        /// <summary>
        /// The name of the default indexing service in <see cref="IndexingServiceReferences"/>
        /// </summary>
        public string DefaultIndexingServiceName { get; set; }

        /// <summary>
        /// Gets and sets whether the default behaviour for filtering should be to include results when no provider is configured for the type. Default = false.
        /// </summary>
        public bool SearchResultFilterDefaultInclude { get; set; } = false;
    }
}
