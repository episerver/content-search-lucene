using System;
using System.Linq;
using EPiServer.Search.Filter;

namespace EPiServer.Search.Configuration.Transform.Internal
{
    public class SearchOptionsTransform
    {
        public static void Transform(SearchConfiguration _section, SearchOptions _options)
        {
            _options.Active = _section.Active;
            _options.QueueFlushInterval = _section.QueueFlushInterval;
            _options.MaxHitsFromIndexingService = _section.MaxHitsFromIndexingService;
            _options.DequeuePageSize = _section.DequeuePageSize;
            _options.DynamicDataStoreName = _section.DynamicDataStoreName;
            _options.IndexingResultDataStoreName = _section.IndexingResultDataStoreName;
            _options.HtmlStripDisplayText = _section.HtmlStripDisplayText;
            _options.HtmlStripMetadata = _section.HtmlStripMetadata;
            _options.HtmlStripTitle = _section.HtmlStripTitle;
            _options.IndexingServiceFieldNameAcl = _section.IndexingServiceFieldNameAcl;
            _options.IndexingServiceFieldNameAuthors = _section.IndexingServiceFieldNameAuthors;
            _options.IndexingServiceFieldNameCategories = _section.IndexingServiceFieldNameCategories;
            _options.IndexingServiceFieldNameCreated = _section.IndexingServiceFieldNameCreated;
            _options.IndexingServiceFieldNameCulture = _section.IndexingServiceFieldNameCulture;
            _options.IndexingServiceFieldNameDefault = _section.IndexingServiceFieldNameDefault;
            _options.IndexingServiceFieldNameDisplayText = _section.IndexingServiceFieldNameDisplayText;
            _options.IndexingServiceFieldNameId = _section.IndexingServiceFieldNameId;
            _options.IndexingServiceFieldNameItemStatus = _section.IndexingServiceFieldNameItemStatus;
            _options.IndexingServiceFieldNameModified = _section.IndexingServiceFieldNameModified;
            _options.IndexingServiceFieldNameTitle = _section.IndexingServiceFieldNameTitle;
            _options.IndexingServiceFieldNameType = _section.IndexingServiceFieldNameType;
            _options.IndexingServiceFieldNameVirtualPath = _section.IndexingServiceFieldNameVirtualPath;
            _options.NamedIndexesUriTemplate = _section.NamedIndexesUriTemplate;
            _options.ResetHttpMethod = _section.ResetHttpMethod;
            _options.ResetUriTemplate = _section.ResetUriTemplate;
            _options.SearchUriTemplate = _section.SearchUriTemplate;
            _options.SyndicationFeedAttributeNameTotalHits = _section.SyndicationFeedAttributeNameTotalHits;
            _options.SyndicationFeedAttributeNameVersion = _section.SyndicationFeedAttributeNameVersion;
            _options.SyndicationItemAttributeNameAutoUpdateVirtualPath = _section.SyndicationItemAttributeNameAutoUpdateVirtualPath;
            _options.SyndicationItemAttributeNameBoostFactor = _section.SyndicationItemAttributeNameBoostFactor;
            _options.SyndicationItemAttributeNameCulture = _section.SyndicationItemAttributeNameCulture;
            _options.SyndicationItemAttributeNameDataUri = _section.SyndicationItemAttributeNameDataUri;
            _options.SyndicationItemAttributeNameIndexAction = _section.SyndicationItemAttributeNameIndexAction;
            _options.SyndicationItemAttributeNameItemStatus = _section.SyndicationItemAttributeNameItemStatus;
            _options.SyndicationItemAttributeNameNamedIndex = _section.SyndicationItemAttributeNameNamedIndex;
            _options.SyndicationItemAttributeNamePublicationEnd = _section.SyndicationItemAttributeNamePublicationEnd;
            _options.SyndicationItemAttributeNamePublicationStart = _section.SyndicationItemAttributeNamePublicationStart;
            _options.SyndicationItemAttributeNameReferenceId = _section.SyndicationItemAttributeNameReferenceId;
            _options.SyndicationItemAttributeNameScore = _section.SyndicationItemAttributeNameScore;
            _options.SyndicationItemAttributeNameType = _section.SyndicationItemAttributeNameType;
            _options.SyndicationItemAttributeNameVersion = _section.SyndicationItemAttributeNameVersion;
            _options.SyndicationItemElementNameAcl = _section.SyndicationItemElementNameAcl;
            _options.SyndicationItemElementNameMetadata = _section.SyndicationItemElementNameMetadata;
            _options.SyndicationItemElementNameVirtualPath = _section.SyndicationItemElementNameVirtualPath;
            _options.UpdateUriTemplate = _section.UpdateUriTemplate;
            _options.UseIndexingServicePaging = _options.UseIndexingServicePaging;
            _options.XmlQualifiedNamespace = _section.XmlQualifiedNamespace;

            _options.SearchResultFilterDefaultInclude = (_section.SearchResultFilter?.DefaultInclude).GetValueOrDefault();
            foreach (var filterProvider in _section.SearchResultFilter?.Providers?.OfType<ProviderElement>() ?? Enumerable.Empty<ProviderElement>())
            {
                _options.FilterProviders.Add(filterProvider.Name, (s) =>
                {
                    var providerType = Type.GetType(filterProvider.Type);
                    if (providerType == null)
                    {
                        throw new ApplicationException(string.Format("The search result filter provider type does not exist for provider with name '{0}'.", filterProvider.Type));
                    }

                    var pInfo = providerType.GetProperty("Instance");
                    if (pInfo == null)
                    {
                        throw new ApplicationException(string.Format("The Instance property could not be found for provider with name '{0}'.", filterProvider.Type));
                    }

                    var provider = (SearchResultFilterProvider)pInfo.GetValue(null, null);
                    if (provider == null)
                    {
                        throw new ApplicationException(string.Format("The Instance property is null for provider with name '{0}'.", filterProvider.Type));
                    }

                    return provider;
                });
            }

            _options.DefaultIndexingServiceName = _section.NamedIndexingServices?.DefaultService;
            foreach (var indexingReference in _section.NamedIndexingServices?.Services?.OfType<IndexingServiceReference>() ?? Enumerable.Empty<IndexingServiceReference>())
            {
                var reference = new IndexingServiceReference()
                {
                    AccessKey = indexingReference.AccessKey,
                    BaseUri = indexingReference.BaseUri,
                    Name = indexingReference.Name
                };

                _options.IndexingServiceReferences.Add(reference);
            }
        }
    }
}
