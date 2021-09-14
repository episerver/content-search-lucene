using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using EPiServer.Logging;
using EPiServer.Models;
using EPiServer.Search.Configuration;
using EPiServer.Search.Filter;

namespace EPiServer.Search.Internal
{
    /// <summary>
    /// Communicates with REST endpoint
    /// </summary>
    public class RequestHandler
    {
        private static ILogger _log = LogManager.GetLogger();
        private readonly SearchOptions _options = SearchSettings.Options;

        public RequestHandler()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public virtual bool SendRequest(FeedModel feed, string namedIndexingService)
        {
            var serviceReference = GetNamedIndexingServiceReference(namedIndexingService, false);

            using (var stream = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream, new UnicodeEncoding()))
                {
                    string jsonData = JsonSerializer.Serialize(feed);
                    sw.Write(jsonData);

                    string url = serviceReference.BaseUri + _options.UpdateUriTemplate.Replace("{accessKey}", serviceReference.AccessKey);

                    try
                    {
                        MakeHttpRequest(url, "POST", serviceReference, jsonData, null).Wait();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Update batch could not be sent to service uri '{url}'", ex);
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Wipes the passed named index and re-creates it
        /// </summary>
        /// <param name="namedIndexingService">The configured indexing service to reset</param>
        /// <param name="namedIndex">The named index to re-create</param>
        public void ResetIndex(string namedIndexingService, string namedIndex)
        {
            var serviceReference = GetNamedIndexingServiceReference(namedIndexingService);

            var parameterMapper = new Dictionary<string, string>();
            parameterMapper.Add("{namedindex}", namedIndex);
            parameterMapper.Add("{accessKey}", serviceReference.AccessKey);

            string url = _options.ResetUriTemplate;
            foreach (string key in parameterMapper.Keys)
            {
                url = url.Replace(key, WebUtility.UrlEncode(parameterMapper[key]));
            }

            url = serviceReference.BaseUri + url;

            try
            {
                MakeHttpRequest(url, _options.ResetHttpMethod, serviceReference, null, null).Wait();
            }
            catch (Exception e)
            {
                _log.Error(string.Format("Could not reset index '{0}' for service uri '{1}'. Message: {2}{3}", namedIndex, url, e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Gets all configured named indexes in the indexing service
        /// </summary>
        /// <param name="namedIndexingService">The configured indexing service from where to get named indexes</param>
        /// <returns></returns>
        protected internal virtual Collection<string> GetNamedIndexes(string namedIndexingService)
        {
            var serviceReference = GetNamedIndexingServiceReference(namedIndexingService);

            var parameterMapper = new Dictionary<string, string>();
            parameterMapper.Add("{accesskey}", serviceReference.AccessKey);

            string url = _options.NamedIndexesUriTemplate;
            foreach (string key in parameterMapper.Keys)
            {
                url = url.Replace(key, WebUtility.UrlEncode(parameterMapper[key]));
            }

            url = serviceReference.BaseUri + url;

            var namedIndexes = new Collection<string>();

            try
            {
                var responseString = MakeHttpRequest(url, "GET", serviceReference, null, null).Result;
                var feeds = JsonSerializer.Deserialize<FeedModel>(responseString);
                foreach (var feed in feeds.Items)
                {
                    namedIndexes.Add(feed.Title);
                }
            }
            catch (Exception e)
            {
                _log.Error(string.Format("Could not get named indexes for uri '{0}'. Message: {1}{2}", url, e.Message, e.StackTrace));
            }

            return namedIndexes;
        }

        /// <summary>
        /// Gets search results for the passed query expression and named indexes to search
        /// </summary>
        /// <param name="query">The plain text query expression to send to indexing service</param>
        /// <param name="namedIndexingService">The configured indexing service to query</param>
        /// <param name="namedIndexes">List of named indexes to search in</param>
        /// <param name="offset">The starting hit for the results, used for paging.</param>
        /// <param name="limit">The number of hits returned, used for paging.</param>
        /// <returns></returns>
        protected internal virtual SearchResults GetSearchResults(string query, string namedIndexingService, Collection<string> namedIndexes, int offset, int limit)
        {
            var parameterMapper = new Dictionary<string, string>();
            var results = new SearchResults();

            var serviceReference = GetNamedIndexingServiceReference(namedIndexingService);

            string indexes = string.Empty;
            if (namedIndexes != null && namedIndexes.Count > 0)
            {
                foreach (string s in namedIndexes)
                    indexes += s + "|";
                indexes = indexes.Substring(0, indexes.LastIndexOf("|", StringComparison.Ordinal));
            }

            parameterMapper.Add("{q}", query);
            parameterMapper.Add("{namedIndexes}", indexes);
            parameterMapper.Add("{accessKey}", serviceReference.AccessKey);

            parameterMapper.Add("{offset}", (_options.UseIndexingServicePaging ? offset.ToString(CultureInfo.InvariantCulture.NumberFormat) : "0"));
            parameterMapper.Add("{limit}", (_options.UseIndexingServicePaging ? limit.ToString(CultureInfo.InvariantCulture.NumberFormat) : _options.MaxHitsFromIndexingService.ToString(CultureInfo.InvariantCulture.NumberFormat)));

            string url = _options.SearchUriTemplate;
            foreach (string key in parameterMapper.Keys)
            {
                url = url.Replace(key, WebUtility.UrlEncode(parameterMapper[key]));
            }

            url = serviceReference.BaseUri + url;

            _log.Debug(string.Format("Start get search results from service with url '{0}'", url));

            try
            {
                var responseString = MakeHttpRequest(url, "GET", serviceReference, null, null).Result;

                results = PopulateSearchResultsFromFeed(responseString, offset, limit);

                //MakeHttpRequest(url, "GET", serviceReference, null, (response) =>
                //{
                //    results = PopulateSearchResultsFromFeed(response, offset, limit);
                //});
            }
            catch (Exception e)
            {
                _log.Error(string.Format("Could not get search results for uri '{0}'. Message: {1}{2}", url, e.Message, e.StackTrace));
                return results;
            }

            _log.Debug(string.Format("End get search results"));

            return results;
        }

        private IndexingServiceReference GetNamedIndexingServiceReference(string name, bool fallbackToDefault = true)
        {
            // Use default indexing service name if passed serviceName is null or empty
            if (string.IsNullOrEmpty(name) && fallbackToDefault)
            {
                name = _options.DefaultIndexingServiceName;
                if (string.IsNullOrEmpty(name))
                {
                    throw new InvalidOperationException("Cannot fallback to default indexing service since it is not defined (defaultService in configuration)");
                }
            }

            var reference = _options.IndexingServiceReferences.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (reference == null)
            {
                throw new ArgumentException($"The named indexing service '{name}' is not defined in the configuration");
            }

            return reference;
        }

        private SearchResults PopulateSearchResultsFromFeed(string response, int offset, int limit)
        {
            FeedModel feeds = JsonSerializer.Deserialize<FeedModel>(response);

            var resultsFiltered = new SearchResults();
            int totalHits = 0;

            int.TryParse(feeds.AttributeExtensions[_options.SyndicationFeedAttributeNameTotalHits], out totalHits);
            string version = feeds.AttributeExtensions[_options.SyndicationFeedAttributeNameVersion];

            foreach (FeedItemModel feed in feeds.Items)
            {
                try
                {
                    var item = new IndexResponseItem(feed.Id);
                    item.Title = feed.Title;
                    item.DisplayText = feed.DisplayText;
                    item.Created = feed.Created;
                    item.Modified = feed.Modified;
                    item.Uri = feed.Uri;

                    item.Culture = feed.AttributeExtensions[_options.SyndicationItemAttributeNameCulture];
                    item.ItemType = feed.AttributeExtensions[_options.SyndicationItemAttributeNameType];
                    item.NamedIndex = feed.AttributeExtensions[_options.SyndicationItemAttributeNameNamedIndex];
                    item.Metadata = feed.AttributeExtensions[_options.SyndicationItemElementNameMetadata];

                    DateTime publicationEnd;
                    DateTime.TryParse(feed.AttributeExtensions[_options.SyndicationItemAttributeNamePublicationEnd], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out publicationEnd);
                    item.PublicationEnd = publicationEnd;

                    DateTime publicationStart;
                    DateTime.TryParse(feed.AttributeExtensions[_options.SyndicationItemAttributeNamePublicationStart], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out publicationStart);
                    item.PublicationStart = publicationStart;

                    //Boost factor
                    float fltBoostFactor = 1;
                    item.BoostFactor = (float.TryParse(feed.AttributeExtensions[_options.SyndicationItemAttributeNameBoostFactor], out fltBoostFactor)) ? fltBoostFactor : 1;

                    // Data Uri
                    Uri uri = null;
                    item.DataUri = ((Uri.TryCreate(feed.AttributeExtensions[_options.SyndicationItemAttributeNameDataUri],
                        UriKind.RelativeOrAbsolute, out uri)) ? uri : null);

                    //Score
                    float score = 0;
                    item.Score = (float.TryParse(feed.AttributeExtensions[_options.SyndicationItemAttributeNameScore], out score)) ? score : 0;


                    foreach (string author in feed.Authors)
                    {
                        item.Authors.Add(author);
                    }

                    foreach (string category in feed.Categories)
                    {
                        item.Categories.Add(category);
                    }

                    foreach (var acl in (Collection<string>)feed.ElementExtensions[_options.SyndicationItemElementNameAcl])
                    {
                        item.AccessControlList.Add(acl);
                    }

                    foreach (var virtualpath in (Collection<string>)feed.ElementExtensions[_options.SyndicationItemElementNameVirtualPath])
                    {
                        item.VirtualPathNodes.Add(virtualpath);
                    }

                    if (SearchResultFilterHandler.Include(item))
                    {
                        resultsFiltered.IndexResponseItems.Add(item);
                    }
                }
                catch (Exception e)
                {
                    _log.Error(string.Format("Could not populate search results for syndication item with id '{0}'. Message: {1}{2}", feed.Id, e.Message, e.StackTrace));
                }
            }

            // if we are using server side paging we can return filtered result using total hits returned from service 
            if (_options.UseIndexingServicePaging)
            {
                resultsFiltered.TotalHits = totalHits;
                resultsFiltered.Version = version;
                return resultsFiltered;
            }

            // If we are using client paging we need to page the filtered results
            var resultsPaged = new SearchResults();
            resultsPaged.TotalHits = resultsFiltered.IndexResponseItems.Count;
            foreach (var item in resultsFiltered.IndexResponseItems.Skip(offset).Take(limit))
            {
                resultsPaged.IndexResponseItems.Add(item);
            }

            resultsPaged.Version = version;
            return resultsPaged;
        }

        internal async virtual System.Threading.Tasks.Task<string> MakeHttpRequest(string url, string method, IndexingServiceReference indexingServiceReference, string postData = null, Action<Stream> responseStreamHandler = null)
        {
            using (var client = new HttpClient())
            {
                if (method == "POST" || method == "DELETE" || method == "PUT")
                {
                    var content = new StringContent(
                        postData,
                        Encoding.UTF8,
                        "application/json"
                    );
                    var result = await client.PostAsync(url, content);
                    return await result.Content.ReadAsStringAsync();
                }
                else
                {
                    var result = await client.GetAsync(url);
                    return await result.Content.ReadAsStringAsync();
                }
            }
        }
    }
}
