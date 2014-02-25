using System;
using System.Globalization;
using System.ServiceModel.Activation;
using System.ServiceModel.Syndication;
using EPiServer.Search.IndexingService.Security;

namespace EPiServer.Search.IndexingService
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class IndexingService : IIndexingService
    {
        /// <summary>
        /// Updates the index
        /// </summary>
        /// <param name="accesskey">The access key used to auhorize this request</param>
        /// <param name="formatter">The feed to process</param>
       
        public virtual void UpdateIndex(string accessKey, SyndicationFeedFormatter formatter)
        {
            if (!Security.SecurityHandler.Instance.IsAuthenticated(accessKey, AccessLevel.Modify))
            {
                IndexingServiceSettings.SetResponseHeaderStatusCode(401);
                return;
            }

            IndexingServiceHandler.Instance.UpdateIndex(formatter.Feed);
        }

        /// <summary>
        /// Gets search results for the query expression q
        /// </summary>
        /// <param name="q">The query expression to parse</param>
        /// <param name="namedIndexes">A pipe separated list of named indexes to search</param>
        /// <param name="offset">The offset from hit 1 to start collection hits from</param>
        /// <param name="limit">The number of items from offset to collect</param>
        /// <param name="accesskey">The accesskey used to authorize the request</param>
        public virtual SyndicationFeedFormatter GetSearchResultsXml(string q, string namedIndexes, string offset, string limit, string accessKey)
        {
            if (!Security.SecurityHandler.Instance.IsAuthenticated(accessKey, AccessLevel.Read))
            {
                IndexingServiceSettings.SetResponseHeaderStatusCode(401);
                return null;
            }

            return GetSearchResults(q, namedIndexes, Int32.Parse(offset, CultureInfo.InvariantCulture), Int32.Parse(limit, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Gets search results for the query expression q
        /// </summary>
        /// <param name="q">The query expression to parse</param>
        /// <param name="namedIndexes">A pipe separated list of named indexes to search</param>
        /// <param name="offset">The offset from hit 1 to start collection hits from</param>
        /// <param name="limit">The number of items from offset to collect</param>
        /// <param name="accesskey">The accesskey used to authorize the request</param>
        public SyndicationFeedFormatter GetSearchResultsJson(string q, string namedIndexes, string offset, string limit, string accessKey)
        {
            if (!Security.SecurityHandler.Instance.IsAuthenticated(accessKey, AccessLevel.Read))
            {
                IndexingServiceSettings.SetResponseHeaderStatusCode(401);
                return null;
            }

            return GetSearchResults(q, namedIndexes, Int32.Parse(offset, CultureInfo.InvariantCulture), Int32.Parse(limit, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Wipes and re-creates the index. NOTE: All files associated with the index will be deleted.
        /// </summary>
        /// <param name="namedIndex">The named index to reset</param>
        /// <param name="accesskey">The client access key used for this request</param>
        public void ResetIndex(string namedIndex, string accessKey)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Reset of index: {0} requested", namedIndex));

            if (!Security.SecurityHandler.Instance.IsAuthenticated(accessKey, AccessLevel.Modify))
            {
                IndexingServiceSettings.SetResponseHeaderStatusCode(401);
                return;
            }
            
            IndexingServiceHandler.Instance.ResetNamedIndex(namedIndex);
        }

        /// <summary>
        /// Gets the configured index names
        /// </summary>
        /// <param name="accesskey">The client access key used for this request</param>
        /// <returns></returns>
        public SyndicationFeedFormatter GetNamedIndexes(string accessKey)
        {
            if (!Security.SecurityHandler.Instance.IsAuthenticated(accessKey, AccessLevel.Read))
            {
                IndexingServiceSettings.SetResponseHeaderStatusCode(401);
                return null;
            }

            return IndexingServiceHandler.Instance.GetNamedIndexes();
        }

        #region Private

        private SyndicationFeedFormatter GetSearchResults(string q, string namedIndexes, int offset, int limit)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Request for search with query parser with expression: {0} in named indexes: {1}", q, namedIndexes));

            //Parse named indexes string from request
            string[] namedIndexesArr = null;
            if (!String.IsNullOrEmpty(namedIndexes))
            {
                char[] delimiter = { '|' };
                namedIndexesArr = namedIndexes.Split(delimiter);
            }

            return IndexingServiceHandler.Instance.GetSearchResults(q, namedIndexesArr, offset, limit);
        }

        #endregion

        #region Events

        public static event EventHandler DocumentAdding;
        public static event EventHandler DocumentAdded;
        public static event EventHandler DocumentRemoving;
        public static event EventHandler DocumentRemoved;
        public static event EventHandler IndexOptimized;
        public static event EventHandler InternalServerError;

        internal static void OnDocumentAdding(object sender, AddUpdateEventArgs e)
        {
            if (DocumentAdding != null)
            {
                DocumentAdding(sender, e);
            }
        }

        internal static void OnDocumentAdded(object sender, AddUpdateEventArgs e)
        {
            if (DocumentAdded != null)
            {
                DocumentAdded(sender, e);
            }
        }

        internal static void OnDocumentRemoving(object sender, RemoveEventArgs e)
        {
            if (DocumentRemoving != null)
            {
                DocumentRemoving(sender, e);
            }
        }

        internal static void OnDocumentRemoved(object sender, RemoveEventArgs e)
        {
            if (DocumentRemoved != null)
            {
                DocumentRemoved(sender, e);
            }
        }

        internal static void OnIndexedOptimized(object sender, OptimizedEventArgs e)
        {
            if (IndexOptimized != null)
            {
                IndexOptimized(sender, e);
            }
        }

    

        internal static void OnInternalServerError(object sender, InternalServerErrorEventArgs e)
        {
            if (InternalServerError != null)
            {
                InternalServerError(sender, e);
            }
        }


        #endregion
    }
}
