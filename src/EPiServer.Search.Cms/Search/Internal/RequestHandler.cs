using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using EPiServer.Logging;
//using EPiServer.Search.Configuration;
using EPiServer.Search.Filter;
using EPiServer.Web;

namespace EPiServer.Search.Internal
{
    /// <summary>
    /// Communicates with REST endpoint
    /// </summary>
    public class RequestHandler
    {
        private static ILogger _log = LogManager.GetLogger();
        private readonly SearchOptions _options;

        public RequestHandler(SearchOptions options)
        {
            _options = options ?? new SearchOptions();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public virtual bool SendRequest(SyndicationFeed feed, string namedIndexingService)
        {
            return true;
            //TO BE UPDATED
            /*var serviceReference = GetNamedIndexingServiceReference(namedIndexingService, false);

            using (var stream = new MemoryStream())
            {
                var settings = new XmlWriterSettings();
                settings.Encoding = System.Text.Encoding.UTF8;
                settings.CheckCharacters = false;
                settings.CloseOutput = false; // This is the default, but setting this explicitly to show why suppressing the CA2202 warning is safe

                using (var writer = XmlWriter.Create(stream, settings))
                {
                    feed.GetAtom10Formatter().WriteTo(writer);
                    writer.Flush();
                }
                stream.Position = 0;

                string url = serviceReference.BaseUri + _options.UpdateUriTemplate.Replace("{accesskey}", serviceReference.AccessKey);

                try
                {
                    MakeHttpRequest(url, "POST", serviceReference, stream, null);

                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error($"Update batch could not be sent to service uri '{url}'", ex);
                    return false;
                }
            } */
        }

        /// <summary>
        /// Wipes the passed named index and re-creates it
        /// </summary>
        /// <param name="namedIndexingService">The configured indexing service to reset</param>
        /// <param name="namedIndex">The named index to re-create</param>
        protected internal virtual void ResetIndex(string namedIndexingService, string namedIndex)
        {
            // TO BE UPDATED

            //var serviceReference = GetNamedIndexingServiceReference(namedIndexingService);

            //var parameterMapper = new Dictionary<string, string>();
            //parameterMapper.Add("{namedindex}", namedIndex);
            //parameterMapper.Add("{accesskey}", serviceReference.AccessKey);

            //string url = _options.ResetUriTemplate;
            //foreach (string key in parameterMapper.Keys)
            //{
            //    url = url.Replace(key, WebUtility.UrlEncode(parameterMapper[key]));
            //}

            //url = serviceReference.BaseUri + url;

            //try
            //{
            //    MakeHttpRequest(url, _options.ResetHttpMethod, serviceReference, null, null);
            //}
            //catch (Exception e)
            //{
            //    _log.Error(string.Format("Could not reset index '{0}' for service uri '{1}'. Message: {2}{3}", namedIndex, url, e.Message, e.StackTrace));
            //}
        }

        /// <summary>
        /// Gets all configured named indexes in the indexing service
        /// </summary>
        /// <param name="namedIndexingService">The configured indexing service from where to get named indexes</param>
        /// <returns></returns>
        protected internal virtual Collection<string> GetNamedIndexes(string namedIndexingService)
        {
            // TO BE UPDATED
            return new Collection<string>();
            //var serviceReference = GetNamedIndexingServiceReference(namedIndexingService);

            //var parameterMapper = new Dictionary<string, string>();
            //parameterMapper.Add("{accesskey}", serviceReference.AccessKey);

            //string url = _options.NamedIndexesUriTemplate;
            //foreach (string key in parameterMapper.Keys)
            //{
            //    url = url.Replace(key, WebUtility.UrlEncode(parameterMapper[key]));
            //}

            //url = serviceReference.BaseUri + url;

            //XmlReader xmlReader = null;
            //SyndicationFeed feed = null;
            //var namedIndexes = new Collection<string>();

            //try
            //{
            //    MakeHttpRequest(url, "GET", serviceReference, null, (response) =>
            //    {
            //        xmlReader = XmlReader.Create(response);
            //        feed = SyndicationFeed.Load(xmlReader);
            //        foreach (var item in feed.Items)
            //        {
            //            namedIndexes.Add(item.Title.Text);
            //        }
            //    });
            //}
            //catch (Exception e)
            //{
            //    _log.Error(string.Format("Could not get named indexes for uri '{0}'. Message: {1}{2}", url, e.Message, e.StackTrace));
            //}

            //return namedIndexes;
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
            // TO BE UPDATED
            return new SearchResults();
            //var parameterMapper = new Dictionary<string, string>();
            //var results = new SearchResults();

            //var serviceReference = GetNamedIndexingServiceReference(namedIndexingService);

            //string indexes = string.Empty;
            //if (namedIndexes != null && namedIndexes.Count > 0)
            //{
            //    foreach (string s in namedIndexes)
            //        indexes += s + "|";
            //    indexes = indexes.Substring(0, indexes.LastIndexOf("|", StringComparison.Ordinal));
            //}

            //parameterMapper.Add("{q}", query);
            //parameterMapper.Add("{namedindexes}", indexes);
            //parameterMapper.Add("{accesskey}", serviceReference.AccessKey);

            //parameterMapper.Add("{offset}", (_options.UseIndexingServicePaging ? offset.ToString(CultureInfo.InvariantCulture.NumberFormat) : "0"));
            //parameterMapper.Add("{limit}", (_options.UseIndexingServicePaging ? limit.ToString(CultureInfo.InvariantCulture.NumberFormat) : _options.MaxHitsFromIndexingService.ToString(CultureInfo.InvariantCulture.NumberFormat)));

            //string url = _options.SearchUriTemplate;
            //foreach (string key in parameterMapper.Keys)
            //{
            //    url = url.Replace(key, WebUtility.UrlEncode(parameterMapper[key]));
            //}

            //url = serviceReference.BaseUri + url;

            //XmlTextReader xmlReader = null;

            //_log.Debug(string.Format("Start get search results from service with url '{0}'", url));

            //try
            //{
            //    MakeHttpRequest(url, "GET", serviceReference, null, (response) =>
            //    {
            //        xmlReader = new XmlTextReader(response);
            //        xmlReader.DtdProcessing = DtdProcessing.Prohibit;
            //        xmlReader.Normalization = false;
            //        var feed = SyndicationFeed.Load(xmlReader);
            //        results = PopulateSearchResultsFromFeed(feed, offset, limit);
            //    });
            //}
            //catch (Exception e)
            //{
            //    _log.Error(string.Format("Could not get search results for uri '{0}'. Message: {1}{2}", url, e.Message, e.StackTrace));
            //    return results;
            //}

            //_log.Debug(string.Format("End get search results"));

            //return results;
        }

        // TO BE UPDATED

        //private IndexingServiceReference GetNamedIndexingServiceReference(string name, bool fallbackToDefault = true)
        //{
        //    // Use default indexing service name if passed serviceName is null or empty
        //    if (string.IsNullOrEmpty(name) && fallbackToDefault)
        //    {
        //        name = _options.DefaultIndexingServiceName;
        //        if (string.IsNullOrEmpty(name))
        //        {
        //            throw new InvalidOperationException("Cannot fallback to default indexing service since it is not defined (defaultService in configuration)");
        //        }
        //    }

        //    var reference = _options.IndexingServiceReferences.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        //    if (reference == null)
        //    {
        //        throw new ArgumentException($"The named indexing service '{name}' is not defined in the configuration");
        //    }

        //    return reference;
        //}

        private SearchResults PopulateSearchResultsFromFeed(SyndicationFeed feed, int offset, int limit)
        {
            var resultsFiltered = new SearchResults();
            int totalHits = 0;
            int.TryParse(GetAttributeValue(feed, _options.SyndicationFeedAttributeNameTotalHits), out totalHits);
            string version = GetAttributeValue(feed, _options.SyndicationFeedAttributeNameVersion);
            foreach (var syndicationItem in feed.Items)
            {
                try
                {
                    var item = new IndexResponseItem(syndicationItem.Id);
                    item.Title = (syndicationItem.Title != null) ? syndicationItem.Title.Text : null;
                    item.DisplayText = (((TextSyndicationContent)syndicationItem.Content) != null) ? ((TextSyndicationContent)syndicationItem.Content).Text : null;
                    item.Created = syndicationItem.PublishDate.DateTime;
                    item.Modified = syndicationItem.LastUpdatedTime.DateTime;
                    item.Uri = syndicationItem.BaseUri;
                    item.Culture = GetAttributeValue(syndicationItem, _options.SyndicationItemAttributeNameCulture);
                    item.ItemType = GetAttributeValue(syndicationItem, _options.SyndicationItemAttributeNameType); ;
                    item.NamedIndex = GetAttributeValue(syndicationItem, _options.SyndicationItemAttributeNameNamedIndex);
                    item.Metadata = GetElementValue(syndicationItem, _options.SyndicationItemElementNameMetadata);

                    DateTime publicationEnd;
                    if (DateTime.TryParse(GetAttributeValue(syndicationItem, _options.SyndicationItemAttributeNamePublicationEnd), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out publicationEnd))
                    {
                        item.PublicationEnd = publicationEnd;
                    }

                    DateTime publicationStart;
                    if (DateTime.TryParse(GetAttributeValue(syndicationItem, _options.SyndicationItemAttributeNamePublicationStart), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out publicationStart))
                    {
                        item.PublicationStart = publicationStart;
                    }

                    //Boost factor
                    float fltBoostFactor = 1;
                    item.BoostFactor =
                        ((float.TryParse(GetAttributeValue(syndicationItem, _options.SyndicationItemAttributeNameBoostFactor),
                        out fltBoostFactor)) ? fltBoostFactor : 1);

                    // Data Uri
                    Uri uri = null;
                    item.DataUri = ((Uri.TryCreate(GetAttributeValue(syndicationItem, _options.SyndicationItemAttributeNameDataUri),
                        UriKind.RelativeOrAbsolute, out uri)) ? uri : null);

                    //Score
                    float score = 0;
                    item.Score = ((float.TryParse(GetAttributeValue(syndicationItem, _options.SyndicationItemAttributeNameScore), out score)) ? score : 0);

                    AddAuthorsToIndexItem(syndicationItem, item);
                    AddCategoriesToIndexItem(syndicationItem, item);
                    AddACLToIndexItem(syndicationItem, item);
                    AddVirtualPathToIndexItem(syndicationItem, item);

                    if (SearchResultFilterHandler.Include(item))
                    {
                        resultsFiltered.IndexResponseItems.Add(item);
                    }
                }
                catch (Exception e)
                {
                    _log.Error(string.Format("Could not populate search results for syndication item with id '{0}'. Message: {1}{2}", syndicationItem.Id, e.Message, e.StackTrace));
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

        private void AddAuthorsToIndexItem(SyndicationItem syndicationItem, IndexItemBase item)
        {
            if (syndicationItem.Authors != null)
            {
                foreach (var person in syndicationItem.Authors)
                {
                    item.Authors.Add(person.Name);
                }
            }
        }

        private void AddCategoriesToIndexItem(SyndicationItem syndicationItem, IndexItemBase item)
        {
            if (syndicationItem.Categories != null)
            {
                foreach (var category in syndicationItem.Categories)
                {
                    item.Categories.Add(category.Name);
                }
            }
        }

        private void AddACLToIndexItem(SyndicationItem syndicationItem, IndexItemBase item)
        {
            var elements = syndicationItem.ElementExtensions.ReadElementExtensions<XElement>
                    (_options.SyndicationItemElementNameAcl,
                    _options.XmlQualifiedNamespace);

            if (elements.Count > 0)
            {
                var element = elements.ElementAt<XElement>(0);
                foreach (var e in element.Elements())
                {
                    item.AccessControlList.Add(e.Value);
                }
            }
        }

        private void AddVirtualPathToIndexItem(SyndicationItem syndicationItem, IndexItemBase item)
        {
            var elements = syndicationItem.ElementExtensions.ReadElementExtensions<XElement>
                    (_options.SyndicationItemElementNameVirtualPath,
                    _options.XmlQualifiedNamespace);

            if (elements.Count > 0)
            {
                var element = elements.ElementAt<XElement>(0);
                foreach (var e in element.Elements())
                {
                    item.VirtualPathNodes.Add(e.Value);
                }
            }
        }

        private string GetAttributeValue(SyndicationItem syndicationItem, string attributeName)
        {
            string value = string.Empty;
            if (syndicationItem.AttributeExtensions.ContainsKey(new XmlQualifiedName(attributeName, _options.XmlQualifiedNamespace)))
            {
                value = syndicationItem.AttributeExtensions[new XmlQualifiedName(attributeName, _options.XmlQualifiedNamespace)];
            }
            return value;
        }

        private string GetAttributeValue(SyndicationFeed syndicationFeed, string attributeName)
        {
            string value = string.Empty;
            if (syndicationFeed.AttributeExtensions.ContainsKey(new XmlQualifiedName(attributeName, _options.XmlQualifiedNamespace)))
            {
                value = syndicationFeed.AttributeExtensions[new XmlQualifiedName(attributeName, _options.XmlQualifiedNamespace)];
            }
            return value;
        }

        private string GetElementValue(SyndicationItem syndicationItem, string elementName)
        {
            var elements = syndicationItem.ElementExtensions.ReadElementExtensions<string>(elementName, _options.XmlQualifiedNamespace);
            string value = "";
            if (elements.Count > 0)
            {
                value = syndicationItem.ElementExtensions.ReadElementExtensions<string>(elementName, _options.XmlQualifiedNamespace).ElementAt<string>(0);
            }

            return value;
        }

        private static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[32768];
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return;
                output.Write(buffer, 0, read);
            }
        }

        // TO BE UPDATED

        //internal virtual void MakeHttpRequest(string url, string method, IndexingServiceReference indexingServiceReference, Stream postData = null, Action<Stream> responseStreamHandler = null)
        //{
        //    var request = WebRequest.Create(url) as HttpWebRequest;
        //    request.UseDefaultCredentials = true;

        //    if (request is HttpWebRequest)
        //    {
        //        var hwr = request;
        //        var cert = indexingServiceReference.GetClientCertificate();
        //        if (cert != null)
        //        {
        //            hwr.ClientCertificates.Add(cert);
        //        }
        //        if (indexingServiceReference.CertificateAllowUntrusted)
        //        {
        //            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
        //            {
        //                return true;
        //            };
        //        }
        //    }

        //    request.Method = method;
        //    if (method == "POST" || method == "DELETE" || method == "PUT")
        //    {
        //        request.ContentType = "application/xml";
        //        if (postData != null)
        //        {
        //            request.ContentLength = postData.Length;

        //            var dataStream = request.GetRequestStream();
        //            CopyStream(postData, dataStream);
        //            dataStream.Close();
        //        }
        //        else
        //        {
        //            request.ContentLength = 0;
        //        }

        //        // Get the response.
        //        var response = request.GetResponse();
        //        responseStreamHandler?.Invoke(response.GetResponseStream());
        //        response.Close();
        //    }
        //    else if (method == "GET")
        //    {
        //        request.ContentType = "application/xml";

        //        var response = request.GetResponse();
        //        responseStreamHandler?.Invoke(response.GetResponseStream());
        //        response.Close();
        //    }
        //}
    }
}
