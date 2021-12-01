using System;
using System.Linq;
using System.Threading;
using EPiServer.Core;
using EPiServer.Core.Internal;
using EPiServer.Data.Dynamic;
using EPiServer.DataAbstraction;
using EPiServer.Logging;
using EPiServer.Search;
using EPiServer.Search.Data;
using EPiServer.ServiceLocation;
using static EPiServer.Search.Initialization.SearchInitialization;

namespace EPiServer.Job
{
    /// <summary>
    /// The default implementation of <see cref="IIndexingJobService"/>.
    /// </summary>
    [ServiceConfiguration(typeof(IIndexingJobService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class IndexingJobService : IIndexingJobService
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(IndexingJobService));
        private static readonly object JobLock = new();

        private readonly IContentRepository _contentRepository;
        private readonly ContentSearchHandler _contentSearchHandler;
        private readonly IContentEvents _contentEvents;
        private readonly SearchHandler _searchHandler;
        private readonly IContentSecurityEvents _contentSecurityEvents;
        private readonly SearchOptions _options;
        private static bool _isInitContentEvent;
        private SearchEventHandler _eventHandler;

        private bool _stop;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexingJobService"/> class.
        /// </summary>
        /// <param name="contentEvents"></param>
        /// <param name="searchHandler"></param>
        /// <param name="contentSecurityEvents"></param>
        /// <param name="contentRepository"></param>
        /// <param name="contentSearchHandler"></param>
        public IndexingJobService(IContentEvents contentEvents,
            SearchHandler searchHandler,
            IContentSecurityEvents contentSecurityEvents,
            ContentRepository contentRepository,
            ContentSearchHandler contentSearchHandler)
        {
            _contentEvents = contentEvents;
            _searchHandler = searchHandler;
            _options = SearchSettings.Options;
            _contentSecurityEvents = contentSecurityEvents;
            _contentRepository = contentRepository;
            _contentSearchHandler = contentSearchHandler;
        }

        /// <summary>
        /// Starts indexing job.
        /// </summary>
        /// <returns>The job report.</returns>
        public virtual string Start() => Start(null);

        /// <summary>
        /// Starts indexing job.
        /// </summary>
        /// <param name="statusNotification">The notification action when job status changed.</param>
        /// <returns>The job report.</returns>
        /// <exception cref="ApplicationException"></exception>
        public virtual string Start(Action<string> statusNotification)
        {
            try
            {
                if (!Monitor.TryEnter(JobLock))
                {
#pragma warning disable CA2201 // Do not raise reserved exception types
                    throw new ApplicationException("Indexing job is already running.");
#pragma warning restore CA2201 // Do not raise reserved exception types
                }

                try
                {
                    var contentSearchHandler = ServiceLocator.Current.GetInstance<ContentSearchHandler>();
                    var requestQueueRemover = new RequestQueueRemover(_searchHandler);

                    IndexAllOnce(contentSearchHandler, requestQueueRemover);

                    InitContentEvent();

                    return "Sucess added to queue";
                }
                finally
                {
                    Monitor.Exit(JobLock);
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
            lock (JobLock)
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
                    Logger.Warning("Error during full search indexing of content and files.", e);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the indexing has been executed before.
        /// </summary>
        /// <value><c>true</c> if indexing was done; otherwise, <c>false</c>.</value>
        private bool HasIndexingBeenExecuted()
        {
            var execution = Store().LoadAll<IndexingInformation>().FirstOrDefault();

            return execution != null && execution.ExecutionDate > DateTime.MinValue;
        }

        /// <summary>
        /// Stops the job.
        /// </summary>
        public virtual void Stop() => _stop = true;

        /// <summary>
        /// Gets stop status of the job.
        /// </summary>
        public virtual bool IsStopped() => _stop;

        private DynamicDataStore Store()
        {
            return DynamicDataStoreFactory.Instance.GetStore(_options.DynamicDataStoreName) ??
                DynamicDataStoreFactory.Instance.CreateStore(_options.DynamicDataStoreName, typeof(IndexRequestQueueItem));
        }

        private void InitContentEvent()
        {
            if (!_isInitContentEvent && SearchSettings.Options.Active)
            {
                _eventHandler = new SearchEventHandler(_contentSearchHandler, _contentRepository);
                _contentEvents.PublishedContent += _eventHandler.ContentEvents_PublishedContent;
                _contentEvents.MovedContent += _eventHandler.ContentEvents_MovedContent;
                _contentEvents.DeletingContent += _eventHandler.ContentEvents_DeletingContent;
                _contentEvents.DeletedContent += _eventHandler.ContentEvents_DeletedContent;
                _contentEvents.DeletedContentLanguage += _eventHandler.ContentEvents_DeletedContentLanguage;
                _contentSecurityEvents.ContentSecuritySaved += _eventHandler.ContentSecurityRepository_Saved;
                PageTypeConverter.PagesConverted += _eventHandler.PageTypeConverter_PagesConverted;
                _isInitContentEvent = true;
            }
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
                foreach (var reIndexable in reIndexables)
                {
                    _log.Information("The RequestQueueRemover truncate queue with NamedIndexingService '{0}' and NamedIndex '{1}'", reIndexable.NamedIndexingService, reIndexable.NamedIndex);
                    _searchHandler.TruncateQueue(reIndexable.NamedIndexingService, reIndexable.NamedIndex);
                }
            }
        }

    }

}
