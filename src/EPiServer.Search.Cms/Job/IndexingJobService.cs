using EPiServer.Core;
using EPiServer.Core.Internal;
using EPiServer.Data.Dynamic;
using EPiServer.Framework;
using EPiServer.Logging;
using EPiServer.Models;
using EPiServer.Search;
using EPiServer.Search.Data;
using EPiServer.Search.Internal;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static EPiServer.Search.Initialization.SearchInitialization;

namespace EPiServer.Job
{
    /// <summary>
    /// The default implementation of <see cref="IIndexingJobService"/>.
    /// </summary>
    [ServiceConfiguration(typeof(IIndexingJobService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class IndexingJobService : IIndexingJobService
    {
        private const string RequestFeedId = "uuid:153f0b26-6ed4-4437-8d47-b381afd5ea2d";
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(IndexingJobService));
        private static readonly object _jobLock = new object();

        private readonly RequestHandler _requestHandler;
        private readonly SearchHandler _searchHandler;
        private readonly ITimeProvider _timeProvider;
        private readonly SearchOptions _options;
        private readonly ContentRepository _contentRepository;
        private static bool IsInitContentEvent = false;
        private SearchEventHandler _eventHandler;

        private bool _stop;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexingJobService"/> class.
        /// </summary>
        /// <param name="siteDefinitionRepository">The site definition repository.</param>
        /// <param name="contentIndexer">The content indexer.</param>
        /// <param name="findConfiguration"></param>
        public IndexingJobService(RequestHandler requestHandler, SearchHandler searchHandler, ITimeProvider timeProvider, ContentRepository contentRepository)
        {
            _requestHandler = requestHandler;
            _searchHandler = searchHandler;
            _options = SearchSettings.Options;
            _timeProvider = timeProvider;
            _contentRepository = contentRepository;
        }

        /// <summary>
        /// Starts indexing job.
        /// </summary>
        /// <returns>The job report.</returns>
        public virtual string Start()
        {
            return Start(null);
        }

        /// <summary>
        /// Starts indexing job.
        /// </summary>
        /// <param name="statusNotification">The notification action when job status changed.</param>
        /// <returns>The job report.</returns>
        public virtual string Start(Action<string> statusNotification)
        {
            try
            {
                if (!Monitor.TryEnter(_jobLock))
                {
                    throw new ApplicationException("Indexing job is already running.");
                }

                try
                {
                    ContentSearchHandler contentSearchHandler = ServiceLocator.Current.GetInstance<ContentSearchHandler>();
                    RequestQueueRemover requestQueueRemover = new RequestQueueRemover(ServiceLocator.Current.GetInstance<SearchHandler>());

                    IndexAllOnce(contentSearchHandler, requestQueueRemover);

                    InitContentEvent();

                    return "Sucess added to queue";
                }
                finally
                {
                    Monitor.Exit(_jobLock);
                }
            }
            finally
            {

                // Release the _stop variable whenever the job is finished.
                _stop = false;
            }
        }

        private void IndexAllOnce(ContentSearchHandler contentSearchHandler, RequestQueueRemover requestQueueRemover)
        {
            lock (_jobLock)
            {
                try
                {
                    if (HasIndexingBeenExecuted())
                    {
                        return;
                    }

                    if (DynamicDataStoreFactory.Instance.GetStore(_options.DynamicDataStoreName) != null)
                    {
                        DynamicDataStoreFactory.Instance.CreateStore(_options.DynamicDataStoreName, typeof(IndexRequestQueueItem));
                    }

                    requestQueueRemover.TruncateQueue(new IReIndexable[] { contentSearchHandler });
                    contentSearchHandler.IndexPublishedContent();

                }
                catch (Exception e)
                {
                    // We do not want any type of exception left unhandled since we are running on a separete thread.
                    _logger.Warning("Error during full search indexing of content and files.", e);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the indexing has been executed before.
        /// </summary>
        /// <value><c>true</c> if indexing was done; otherwise, <c>false</c>.</value>
        private bool HasIndexingBeenExecuted()
        {
            IndexingInformation execution = Store().LoadAll<IndexingInformation>().FirstOrDefault();

            if (execution != null && execution.ExecutionDate > DateTime.MinValue)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Stops the job.
        /// </summary>
        public virtual void Stop()
        {
            _stop = true;
        }

        /// <summary>
        /// Gets stop status of the job.
        /// </summary>
        public virtual bool IsStopped()
        {
            return _stop;
        }

        private DynamicDataStore Store()
        {
            var dataStore = DynamicDataStoreFactory.Instance.GetStore(_options.DynamicDataStoreName) ??
                DynamicDataStoreFactory.Instance.CreateStore(_options.DynamicDataStoreName, typeof(IndexRequestQueueItem));

            return dataStore;
        }

        private void InitContentEvent()
        {
            if (!IsInitContentEvent && SearchSettings.Options.Active)
            {
                var contentRepo = Locate.ContentRepository();
                var contentSecurityRepo = Locate.ContentSecurityRepository();
                var contentEvents = Locate.ContentEvents();

                ContentSearchHandler contentSearchHandler = Locate.Advanced.GetInstance<ContentSearchHandler>();
                _eventHandler = new SearchEventHandler(contentSearchHandler, contentRepo);

                contentEvents.PublishedContent += _eventHandler.ContentEvents_PublishedContent;
                contentEvents.MovedContent += _eventHandler.ContentEvents_MovedContent;
                contentEvents.DeletingContent += _eventHandler.ContentEvents_DeletingContent;
                contentEvents.DeletedContent += _eventHandler.ContentEvents_DeletedContent;
                contentEvents.DeletedContentLanguage += _eventHandler.ContentEvents_DeletedContentLanguage;

                contentSecurityRepo.ContentSecuritySaved += _eventHandler.ContentSecurityRepository_Saved;

                PageTypeConverter.PagesConverted += _eventHandler.PageTypeConverter_PagesConverted;

                IsInitContentEvent = true;
            }
        }

        private ServiceProviderHelper Locate
        {
            get { return new ServiceProviderHelper(ServiceLocator.Current); }
        }

        private class RequestQueueRemover
        {
            private static readonly ILogger _log = LogManager.GetLogger();
            private readonly SearchHandler _searchHandler;

            public RequestQueueRemover(SearchHandler searchHandler)
            {
                _searchHandler = searchHandler;
            }

            public void TruncateQueue(IReIndexable[] reIndexables)
            {
                foreach (IReIndexable reIndexable in reIndexables)
                {
                    _log.Information("The RequestQueueRemover truncate queue with NamedIndexingService '{0}' and NamedIndex '{1}'", reIndexable.NamedIndexingService, reIndexable.NamedIndex);
                    _searchHandler.TruncateQueue(reIndexable.NamedIndexingService, reIndexable.NamedIndex);
                }
            }
        }

    }

}
