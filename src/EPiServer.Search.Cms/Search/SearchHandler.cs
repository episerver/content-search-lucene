using System;
using System.Collections.ObjectModel;
using EPiServer.Search.Internal;
using EPiServer.Search.Queries;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Options;

namespace EPiServer.Search
{
    public class SearchHandler
    {
        private static SearchHandler _instance;

        private readonly RequestHandler _requestHandler;
        private readonly RequestQueueHandler _requestQueueHandler;
        private readonly SearchOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchHandler"/> class.
        /// </summary>
        public SearchHandler(RequestHandler requestHandler, RequestQueueHandler requestQueueHandler, IOptions<SearchOptions> options)
        {
            _requestHandler = requestHandler;
            _requestQueueHandler = requestQueueHandler;
            _options = options.Value;
        }

        [Obsolete("Request the SearchHandler from the service container.")]
        public static SearchHandler Instance
        {
            get { return _instance ?? ServiceLocator.Current.GetInstance<SearchHandler>(); }
            set { _instance = value; }
        }

        /// <summary>
        /// Updates the index with the passed <see cref="IndexRequestItem"/>
        /// </summary>
        /// <param name="item">The <see cref="IndexRequestItem"/> containing data to use when updating the default search index</param>
        public virtual void UpdateIndex(IndexRequestItem item)
        {
            UpdateIndex(item, null);
        }

        /// <summary>
        /// Updates the index with the passed <see cref="IndexRequestItem"/>
        /// </summary>
        /// <param name="item">The <see cref="IndexRequestItem"/> containing data to use when updating the search index</param>
        /// <param name="namedIndexingService">The name for the configured indexing service to call for this <see cref="IndexRequestItem"/></param>
        public virtual void UpdateIndex(IndexRequestItem item, string namedIndexingService)
        {
            if (!_options.Active)
                throw new InvalidOperationException("Can not perform this operation when EPiServer.Search is not set as active in configuration");

            if (item == null)
                throw new ArgumentNullException("item");

            if (string.IsNullOrEmpty(item.Id))
                throw new ArgumentException("The Id property cannot be null");

            _requestQueueHandler.Enqueue(item, namedIndexingService);
        }

        /// <summary>
        /// Gets search results for a <see cref="IQueryExpression"/> in the default index
        /// </summary>
        /// <param name="queryExpression">The <see cref="IQueryExpression"/> to send to indexing service</param>
        /// <param name="page">The search result page, starting a 1</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <returns></returns>
        public virtual SearchResults GetSearchResults(IQueryExpression queryExpression, int page, int pageSize)
        {
            return GetSearchResults(queryExpression, null, null, page, pageSize);
        }

        /// <summary>
        /// Gets search results for a <see cref="IQueryExpression"/> and a list of named indexes
        /// </summary>
        /// <param name="queryExpression">The <see cref="IQueryExpression"/> to send to indexing service</param>
        /// <param name="namedIndexingService">The configured named indexing service to query</param>
        /// <param name="namedIndexes">A collection of named indexes to search in. If null, the default index will be used</param>
        /// <param name="page">The search result page, starting a 1</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <returns></returns>
        public virtual SearchResults GetSearchResults(IQueryExpression queryExpression, string namedIndexingService, Collection<string> namedIndexes, int page, int pageSize)
        {
            if (!_options.Active)
                throw new InvalidOperationException("Can not perform this operation when EPiServer.Search is not set as active in configuration");

            if (page <= 0)
            {
                throw new ArgumentOutOfRangeException("page", page, "The search result page cannot be less than 1");
            }
            if (pageSize < 0)
            {
                throw new ArgumentOutOfRangeException("pageSize", page, "The number of results returned can not be less than 0");
            }

            int offset = (page * pageSize) - pageSize;
            int limit = pageSize;

            return _requestHandler.GetSearchResults(queryExpression.GetQueryExpression(), namedIndexingService, namedIndexes, offset, limit);
        }

        /// <summary>
        /// Gets a list of named indexes from the passed indexing service
        /// </summary>
        /// <param name="namedIndexingService">The configured named indexing service from where to get the named indexes</param>
        /// <returns></returns>
        public virtual Collection<string> GetNamedIndexes(string namedIndexingService)
        {
            if (!_options.Active)
                throw new InvalidOperationException("Can not perform this operation when EPiServer.Search is not set as active in configuration");

            return _requestHandler.GetNamedIndexes(namedIndexingService);
        }

        /// <summary>
        /// Gets a list of named indexes from the default indexing service
        /// </summary>
        /// <returns></returns>
        public virtual Collection<string> GetNamedIndexes()
        {
            return GetNamedIndexes(null);
        }

        /// <summary>
        /// Send a request to the passed indexing service to reset the passed named index. NOTE: this wipes and recreates the index.
        /// </summary>
        /// <param name="namedIndexingService">The configured named indexing service to reset</param>
        /// <param name="namedIndex">The named index to reset</param>
        /// <remarks>Reset requests are not enqueued but sent immediately</remarks>
        public virtual void ResetIndex(string namedIndexingService, string namedIndex)
        {
            if (!_options.Active)
                throw new InvalidOperationException("Can not perform this operation when EPiServer.Search is not set as active in configuration");

            _requestHandler.ResetIndex(namedIndexingService, namedIndex);
        }

        /// <summary>
        /// Send a request to the default indexing service to reset the supplied named index. NOTE: this wipes and recreates the index.
        /// </summary>
        /// <param name="namedIndex">The named index to reset</param>
        /// <remarks>Reset requests are not enqueued but sent immediately</remarks>
        public virtual void ResetIndex(string namedIndex)
        {
            ResetIndex(null, namedIndex);
        }

        /// <summary>
        /// Removes the request items from queue (unprocessed items).
        /// </summary>
        /// <param name="namedIndexingService">The named indexing service.</param>
        /// <param name="namedIndex">Index of the named.</param>
        public virtual void TruncateQueue(string namedIndexingService, string namedIndex)
        {
            if (!_options.Active)
                throw new InvalidOperationException("Can not perform this operation when EPiServer.Search is not set as active in configuration");
            _requestQueueHandler.TruncateQueue(namedIndexingService, namedIndex);
        }

        /// <summary>
        /// Reset all index and for each named index Sends a request to the default indexing service to reset the named index.
        /// </summary>
        /// <remarks>
        /// Reset requests are not enqueued but sent immediately
        /// </remarks>
        internal void ResetAllIndex()
        {
            if (!_options.Active)
            {
                return;
            }

            foreach (var namedIndex in GetNamedIndexes())
            {
                ResetIndex(namedIndex);
            }
        }
    }
}
