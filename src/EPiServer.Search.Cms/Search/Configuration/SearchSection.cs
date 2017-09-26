 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using EPiServer.Search.Configuration;
using EPiServer.Framework.Configuration;

namespace EPiServer.Search.Configuration
{
    public class SearchSection : ConfigurationSection
    {
        private static IConfigurationSource _config;
        private static SearchSection _searchSection;
        private static object _syncObject = new object();
        private bool? _useIndexServicePaging = null;
        private int? _queueFlushInterval = null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static SearchSection()
        {
            ConfigurationSource.SourceChanged += (sender, args) => { lock (_syncObject) { _config = null; _searchSection = null; } };
        }

        /// <summary>
        /// Gets and sets the current configuration to use
        /// </summary>
        /// <exclude/>
        [Obsolete("Use ConfigurationSource.Instance = new FileConfigurationSource(value) to replace global configuration or use GlobalConfigurationManager to Load and Save configuration files")]
        public static System.Configuration.Configuration ConfigurationInstance
        {
            get
            {
                EnsureCurrentConfig();
                var source = _config as FileConfigurationSource;
                return source != null ? source.ConfigurationInstance : null;
            }
            set
            {
                lock (_syncObject)
                {
                    _config = new FileConfigurationSource(value);
                    _searchSection = null;
                }
            }
        }

        private static void EnsureCurrentConfig()
        {
            if (_config != null && _searchSection != null)
            {
                return;
            }

            lock (_syncObject)
            {
                if (_config == null)
                {
                    _config = ConfigurationSource.Instance;
                }
                if (_searchSection == null)
                {
                    _searchSection = _config.Get<SearchSection>("episerver.search") ?? new SearchSection();
                }
            }
        }


        /// <summary>
        /// Gets the instance of the <see cref="SearchSection"/> section
        /// </summary>
        public static SearchSection Instance
        {
            get
            {
                EnsureCurrentConfig();
                return _searchSection;
            }
        }

        /// <summary>
        /// Gets and sets whether the full text search service is active or not. No calls will be made to the search index if false.
        /// </summary>
        [ConfigurationProperty("active", IsRequired = true)]
        public bool Active
        {
            get { return (bool)base["active"]; }
            set { base["active"] = value; }
        }

        /// <summary>
        /// Gets and sets a timer interval in seconds when the requests queue to indexing service should be dequeued
        /// </summary>
        [ConfigurationProperty("queueFlushInterval", IsRequired = false, DefaultValue = 30)]
        public int QueueFlushInterval
        {
            get { return _queueFlushInterval ?? (int)base["queueFlushInterval"]; }
            set { if (IsReadOnly()) { _queueFlushInterval = value; } else { base["queueFlushInterval"] = value; } }
        }

        /// <summary>
        /// Gets and sets the maximum number of hits returned by the indexing service
        /// </summary>
        [ConfigurationProperty("maxHitsFromIndexingService", IsRequired = false, DefaultValue = 500)]
        public int MaxHitsFromIndexingService
        {
            get { return (int)base["maxHitsFromIndexingService"]; }
            set { base["maxHitsFromIndexingService"] = value; }
        }

        /// <summary>
        /// Gets and sets whether to send paging parameters passed to GetSearchResults (page and pagesize) to service. If set to false page=1 and pagesize=[MaxHits] will sent.
        /// This should be set to false if any SearchResultFilter is plugged in.
        /// </summary>
        [ConfigurationProperty("useIndexingServicePaging", IsRequired = false, DefaultValue = true)]
        public bool UseIndexingServicePaging
        {
            get { return _useIndexServicePaging ?? (bool)base["useIndexingServicePaging"]; }
            set { if (IsReadOnly()) { _useIndexServicePaging = value; } else { base["useIndexingServicePaging"] = value; } }

        }

        /// <summary>
        /// Gets the name of the Dynamic Data Store
        /// </summary>
        [ConfigurationProperty("dynamicDataStoreName", IsRequired = false, DefaultValue = "IndexRequestQueueDataStore")]
        public string DynamicDataStoreName
        {
            get { return (string)base["dynamicDataStoreName"]; }
            set { base["dynamicDataStoreName"] = value; }
        }

        /// <summary>
        /// Gets and sets the uri template for update reqeuest to the indexing service
        /// </summary>
        [ConfigurationProperty("updateUriTemplate", IsRequired = false, DefaultValue = "/update/?accesskey={accesskey}")]
        public string UpdateUriTemplate
        {
            get { return (string)base["updateUriTemplate"]; }
            set { base["updateUriTemplate"] = value; }
        }

        /// <summary>
        /// Gets and sets the uri template for search requests to the indexing service. The Http method is always GET
        /// Required replaceables: "{q}", {namedIndexes}
        /// </summary>
        [ConfigurationProperty("searchUriTemplate", IsRequired = false, DefaultValue = "/search/?q={q}&namedindexes={namedindexes}&offset={offset}&limit={limit}&format=xml&accesskey={accesskey}")]
        public string SearchUriTemplate
        {
            get { return (string)base["searchUriTemplate"]; }
            set { base["searchUriTemplate"] = value; }
        }

        /// <summary>
        /// Gets and sets the uri template for reset index request to the indexing service
        /// </summary>
        [ConfigurationProperty("resetUriTemplate", IsRequired = false, DefaultValue = "/reset/?namedindex={namedindex}&accesskey={accesskey}")]
        public string ResetUriTemplate
        {
            get { return (string)base["resetUriTemplate"]; }
            set { base["resetUriTemplate"] = value; }
        }

        /// <summary>
        /// Gets and sets the Http method for Reset index requests to the indexing service
        /// </summary>
        [ConfigurationProperty("resetHttpMethod", IsRequired = false, DefaultValue = "POST")]
        public string ResetHttpMethod
        {
            get { return (string)base["resetHttpMethod"]; }
            set { base["resetHttpMethod"] = value; }
        }

        /// <summary>
        /// Gets and sets the uri template for getting all named indexes
        /// </summary>
        [ConfigurationProperty("namedIndexesUriTemplate", IsRequired = false, DefaultValue = "/namedindexes/?accesskey={accesskey}")]
        public string NamedIndexesUriTemplate
        {
            get { return (string)base["namedIndexesUriTemplate"]; }
            set { base["namedIndexesUriTemplate"] = value; }
        }

        /// <summary>
        /// Gets and sets the namespace for XmlQualifiedName
        /// </summary>
        [ConfigurationProperty("xmlQualifiedNamespace", IsRequired = false, DefaultValue = "EPiServer.Search.IndexingService")]
        public string XmlQualifiedNamespace
        {
            get { return (string)base["xmlQualifiedNamespace"]; }
            set { base["xmlQualifiedNamespace"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication feed attribute extension TotalHits
        /// </summary>
        [ConfigurationProperty("syndicationFeedAttributeNameTotalHits", IsRequired = false, DefaultValue = "TotalHits")]
        public string SyndicationFeedAttributeNameTotalHits
        {
            get { return (string)base["syndicationFeedAttributeNameTotalHits"]; }
            set { base["syndicationFeedAttributeNameTotalHits"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication feed attribute extension Version
        /// </summary>
        [ConfigurationProperty("syndicationFeedAttributeNameVersion", IsRequired = false, DefaultValue = "Version")]
        public string SyndicationFeedAttributeNameVersion
        {
            get { return (string)base["syndicationFeedAttributeNameVersion"]; }
            set { base["syndicationFeedAttributeNameVersion"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication item attribute extension Culture
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNameCulture", IsRequired = false, DefaultValue = "Culture")]
        public string SyndicationItemAttributeNameCulture
        {
            get { return (string)base["syndicationItemAttributeNameCulture"]; }
            set { base["syndicationItemAttributeNameCulture"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication item attribute extension Type
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNameType", IsRequired = false, DefaultValue = "Type")]
        public string SyndicationItemAttributeNameType
        {
            get { return (string)base["syndicationItemAttributeNameType"]; }
            set { base["syndicationItemAttributeNameType"] = value; }
        }

        /// <summary>
        /// gets and sets the name for the syndication item attribute extension ReferenceId
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNameReferenceId", IsRequired = false, DefaultValue = "ReferenceId")]
        public string SyndicationItemAttributeNameReferenceId
        {
            get { return (string)base["syndicationItemAttributeNameReferenceId"]; }
            set { base["syndicationItemAttributeNameReferenceId"] = value; }
        }

        /// <summary>
        /// gets and sets the name for the syndication item attribute extension ItemStatus
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNameItemStatus", IsRequired = false, DefaultValue = "ItemStatus")]
        public string SyndicationItemAttributeNameItemStatus
        {
            get { return (string)base["syndicationItemAttributeNameItemStatus"]; }
            set { base["syndicationItemAttributeNameItemStatus"] = value; }
        }

        /// <summary>
        /// gets and sets the name for the syndication item element extension ACL
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Acl"), ConfigurationProperty("syndicationItemElementNameAcl", IsRequired = false, DefaultValue = "ACL")]
        public string SyndicationItemElementNameAcl
        {
            get { return (string)base["syndicationItemElementNameAcl"]; }
            set { base["syndicationItemElementNameAcl"] = value; }
        }

        /// <summary>
        /// gets and sets the name for the syndication item attribute extension VirtualPath
        /// </summary>
        [ConfigurationProperty("syndicationItemElementNameVirtualPath", IsRequired = false, DefaultValue = "VirtualPath")]
        public string SyndicationItemElementNameVirtualPath
        {
            get { return (string)base["syndicationItemElementNameVirtualPath"]; }
            set { base["syndicationItemElementNameVirtualPath"] = value; }
        }


        /// <summary>
        /// Gets and sets the name for the syndication item element extension Metadata
        /// </summary>
        [ConfigurationProperty("syndicationItemElementNameMetadata", IsRequired = false, DefaultValue = "Metadata")]
        public string SyndicationItemElementNameMetadata
        {
            get { return (string)base["syndicationItemElementNameMetadata"]; }
            set { base["syndicationItemElementNameMetadata"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication item attribute extension BoostFactor
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNameBoostFactor", IsRequired = false, DefaultValue = "BoostFactor")]
        public string SyndicationItemAttributeNameBoostFactor
        {
            get { return (string)base["syndicationItemAttributeNameBoostFactor"]; }
            set { base["syndicationItemAttributeNameBoostFactor"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension NamedIndex
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNameNamedIndex", IsRequired = false, DefaultValue = "NamedIndex")]
        public string SyndicationItemAttributeNameNamedIndex
        {
            get { return (string)base["syndicationItemAttributeNameNamedIndex"]; }
            set { base["syndicationItemAttributeNameNamedIndex"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension IndexAction
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNameIndexAction", IsRequired = false, DefaultValue = "IndexAction")]
        public string SyndicationItemAttributeNameIndexAction
        {
            get { return (string)base["syndicationItemAttributeNameIndexAction"]; }
            set { base["syndicationItemAttributeNameIndexAction"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension AutoUpdateVirtualPath
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNameAutoUpdateVirtualPath", IsRequired = false, DefaultValue = "AutoUpdateVirtualPath")]
        public string SyndicationItemAttributeNameAutoUpdateVirtualPath
        {
            get { return (string)base["syndicationItemAttributeNameAutoUpdateVirtualPath"]; }
            set { base["syndicationItemAttributeNameAutoUpdateVirtualPath"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension Version
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNameVersion", IsRequired = false, DefaultValue = "Version")]
        public string SyndicationItemAttributeNameVersion
        {
            get { return (string)base["syndicationItemAttributeNameVersion"]; }
            set { base["syndicationItemAttributeNameVersion"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension CallbackUri
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNameDataUri", IsRequired = false, DefaultValue = "DataUri")]
        public string SyndicationItemAttributeNameDataUri
        {
            get { return (string)base["syndicationItemAttributeNameDataUri"]; }
            set { base["syndicationItemAttributeNameDataUri"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension Score
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNameScore", IsRequired = false, DefaultValue = "Score")]
        public string SyndicationItemAttributeNameScore
        {
            get { return (string)base["syndicationItemAttributeNameScore"]; }
            set { base["syndicationItemAttributeNameScore"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension PublicationEnd
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNamePublicationEnd", IsRequired = false, DefaultValue = "PublicationEnd")]
        public string SyndicationItemAttributeNamePublicationEnd
        {
            get { return (string)base["syndicationItemAttributeNamePublicationEnd"]; }
            set { base["syndicationItemAttributeNamePublicationEnd"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the syndication feed item attribute extension PublicationStart
        /// </summary>
        [ConfigurationProperty("syndicationItemAttributeNamePublicationStart", IsRequired = false, DefaultValue = "PublicationStart")]
        public string SyndicationItemAttributeNamePublicationStart
        {
            get { return (string)base["syndicationItemAttributeNamePublicationStart"]; }
            set { base["syndicationItemAttributeNamePublicationStart"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name ID
        /// </summary>
        [ConfigurationProperty("indexingServiceFieldNameId", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_ID")]
        public string IndexingServiceFieldNameId
        {
            get { return (string)base["indexingServiceFieldNameId"]; }
            set { base["indexingServiceFieldNameId"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name Default
        /// </summary>
        [ConfigurationProperty("indexingServiceFieldNameDefault", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_DEFAULT")]
        public string IndexingServiceFieldNameDefault
        {
            get { return (string)base["indexingServiceFieldNameDefault"]; }
            set { base["indexingServiceFieldNameDefault"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name Title
        /// </summary>
        [ConfigurationProperty("indexingServiceFieldNameTitle", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_TITLE")]
        public string IndexingServiceFieldNameTitle
        {
            get { return (string)base["indexingServiceFieldNameTitle"]; }
            set { base["indexingServiceFieldNameTitle"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name DisplayText
        /// </summary>h
        [ConfigurationProperty("indexingServiceFieldNameDisplayText", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_DISPLAYTEXT")]
        public string IndexingServiceFieldNameDisplayText
        {
            get { return (string)base["indexingServiceFieldNameDisplayText"]; }
            set { base["indexingServiceFieldNameDisplayText"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name Authors
        /// </summary>
        [ConfigurationProperty("indexingServiceFieldNameAuthors", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_AUTHORS")]
        public string IndexingServiceFieldNameAuthors
        {
            get { return (string)base["indexingServiceFieldNameAuthors"]; }
            set { base["indexingServiceFieldNameAuthors"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name Created
        /// </summary>
        [ConfigurationProperty("indexingServiceFieldNameCreated", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_CREATED")]
        public string IndexingServiceFieldNameCreated
        {
            get { return (string)base["indexingServiceFieldNameCreated"]; }
            set { base["indexingServiceFieldNameCreated"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name Modified
        /// </summary>
        [ConfigurationProperty("indexingServiceFieldNameModified", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_MODIFIED")]
        public string IndexingServiceFieldNameModified
        {
            get { return (string)base["indexingServiceFieldNameModified"]; }
            set { base["indexingServiceFieldNameModified"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name Categories
        /// </summary>
        [ConfigurationProperty("indexingServiceFieldNameCategories", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_CATEGORIES")]
        public string IndexingServiceFieldNameCategories
        {
            get { return (string)base["indexingServiceFieldNameCategories"]; }
            set { base["indexingServiceFieldNameCategories"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name Read Access Control List
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Acl"), ConfigurationProperty("indexingServiceFieldNameAcl", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_ACL")]
        public string IndexingServiceFieldNameAcl
        {
            get { return (string)base["indexingServiceFieldNameAcl"]; }
            set { base["indexingServiceFieldNameAcl"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name VirtualPath
        /// </summary>
        [ConfigurationProperty("indexingServiceFieldNameVirtualPath", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_VIRTUALPATH")]
        public string IndexingServiceFieldNameVirtualPath
        {
            get { return (string)base["indexingServiceFieldNameVirtualPath"]; }
            set { base["indexingServiceFieldNameVirtualPath"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name Type
        /// </summary>
        [ConfigurationProperty("indexingServiceFieldNameType", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_TYPE")]
        public string IndexingServiceFieldNameType
        {
            get { return (string)base["indexingServiceFieldNameType"]; }
            set { base["indexingServiceFieldNameType"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name Culture
        /// </summary>
        [ConfigurationProperty("indexingServiceFieldNameCulture", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_CULTURE")]
        public string IndexingServiceFieldNameCulture
        {
            get { return (string)base["indexingServiceFieldNameCulture"]; }
            set { base["indexingServiceFieldNameCulture"] = value; }
        }

        /// <summary>
        /// Gets and sets the name for the indexing service field name ItemStatus
        /// </summary>
        [ConfigurationProperty("indexingServiceFieldNameItemStatus", IsRequired = false, DefaultValue = "EPISERVER_SEARCH_ITEMSTATUS")]
        public string IndexingServiceFieldNameItemStatus
        {
            get { return (string)base["indexingServiceFieldNameItemStatus"]; }
            set { base["indexingServiceFieldNameItemStatus"] = value; }
        }

        /// <summary>
        /// Gets and sets and sets the search result filter element that configures the filter provider to use after retreiving search results from service
        /// </summary>
        [ConfigurationProperty("searchResultFilter", IsRequired = false)]
        public SearchResultFilterElement SearchResultFilterElement
        {
            get { return (SearchResultFilterElement)base["searchResultFilter"]; }
            set { base["searchResultFilter"] = value; }
        }

        /// <summary>
        /// Gets and sets and sets the named indexing services element that configures the available service endpoints
        /// </summary>
        [ConfigurationProperty("namedIndexingServices", IsRequired = true)]
        public NamedIndexingServicesElement NamedIndexingServices
        {
            get { return (NamedIndexingServicesElement)base["namedIndexingServices"]; }
            set { base["namedIndexingServices"] = value; }
        }

        /// <summary>
        /// Gets and sets and sets the page size to use when dequeueing the request queue
        /// </summary>
        [ConfigurationProperty("dequeuePageSize", IsRequired = false, DefaultValue = 50)]
        public int DequeuePageSize
        {
            get { return (int)base["dequeuePageSize"]; }
            set { base["dequeuePageSize"] = value; }
        }

        /// <summary>
        /// Gets and sets and sets whether to automatically strip HTML from the IndexItem Title before sending it to indexing service
        /// </summary>
        [ConfigurationProperty("htmlStripTitle", IsRequired = false, DefaultValue = true)]
        public bool HtmlStripTitle
        {
            get { return (bool)base["htmlStripTitle"]; }
            set { base["htmlStripTitle"] = value; }
        }

        /// <summary>
        /// Gets and sets and sets whether to automatically strip HTML from the IndexItem DisplayText before sending it to indexing service
        /// </summary>
        [ConfigurationProperty("htmlStripDisplayText", IsRequired = false, DefaultValue = true)]
        public bool HtmlStripDisplayText
        {
            get { return (bool)base["htmlStripDisplayText"]; }
            set { base["htmlStripDisplayText"] = value; }
        }

        /// <summary>
        /// Gets and sets and sets whether to automatically strip HTML from the IndexItem Metadata before sending it to indexing service
        /// </summary>
        [ConfigurationProperty("htmlStripMetadata", IsRequired = false, DefaultValue = true)]
        public bool HtmlStripMetadata
        {
            get { return (bool)base["htmlStripMetadata"]; }
            set { base["htmlStripMetadata"] = value; }
        }
    }
} 