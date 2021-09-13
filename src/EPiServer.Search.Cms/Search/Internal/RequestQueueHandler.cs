using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Xml;
using EPiServer.Framework;
using EPiServer.Logging;
using EPiServer.Models;
using EPiServer.Search.Data;

namespace EPiServer.Search.Internal
{
    public class RequestQueueHandler
    {
        private const string RequestFeedId = "uuid:153f0b26-6ed4-4437-8d47-b381afd5ea2d";
        private static object _syncObject = new object();
        private static ILogger _log = LogManager.GetLogger();

        private System.Timers.Timer _queueFlushTimer;
        private readonly RequestHandler _requestHandler;
        private readonly RequestQueue _queue;
        private readonly SearchOptions _options;
        private readonly ITimeProvider _timeProvider;

        public RequestQueueHandler(RequestHandler requestHandler, RequestQueue queue, ITimeProvider timeProvider)
        {
            _requestHandler = requestHandler;
            _queue = queue;
            _options = SearchSettings.Options;
            _timeProvider = timeProvider;

            _queueFlushTimer = new System.Timers.Timer(_options.QueueFlushInterval * 1000)
            {
                AutoReset = false
            };
            _queueFlushTimer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
        }

        public virtual void Enqueue(IndexRequestItem item, string namedIndexingService)
        {
            if (string.IsNullOrEmpty(namedIndexingService))
            {
                namedIndexingService = _options.DefaultIndexingServiceName;
            }

            var queueItem = new IndexRequestQueueItem()
            {
                IndexItemId = item.Id,
                NamedIndex = item.NamedIndex,
                NamedIndexingService = namedIndexingService,
                FeedItemJson = item.ToFeedItemJson(_options),
                Timestamp = DateTime.Now
            };

            _queue.Add(queueItem);
        }

        public virtual void TruncateQueue()
        {
            if (!_options.Active)
                throw new InvalidOperationException("Can not perform this operation when EPiServer.Search is not set as active in configuration");

            _queue.Truncate();
        }

        public virtual void TruncateQueue(string namedIndexingService, string namedIndex)
        {
            if (!_options.Active)
                throw new InvalidOperationException("Can not perform this operation when EPiServer.Search is not set as active in configuration");

            if (string.IsNullOrEmpty(namedIndexingService))
            {
                namedIndexingService = _options.DefaultIndexingServiceName;
            }

            _queue.Truncate(namedIndexingService, namedIndex);
        }

        public virtual void StartQueueFlushTimer()
        {
            _queueFlushTimer.Enabled = true;
        }

        public virtual void ProcessQueue()
        {
            lock (_syncObject)
            {
                int pageSize = _options.DequeuePageSize;

                _log.Debug("Start dequeue unprocessed items");

                // Iterate all configured indexing services
                foreach (var serviceReference in _options.IndexingServiceReferences)
                {
                    while (true)
                    {
                        try
                        {
                            var queueItems = _queue.Get(serviceReference.Name, pageSize);

                            if (!queueItems.Any())
                            {
                                break;
                            }

                            _log.Debug($"Start processing batch for indexing service '{serviceReference.Name}'");

                            var feed = GetUnprocessedFeed(queueItems);

                            bool success = _requestHandler.SendRequest(feed, serviceReference.Name);

                            if (!success)
                            {
                                // The batch could not be sent due to network errors. We should leave the items in queue and move on to the next named service.
                                _log.Error($"Send batch for named index '{serviceReference.Name}' failed. Items are left in queue.");
                                break;
                            }

                            _queue.Remove(queueItems);

                            _log.Debug("End processing batch");
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"RequestQueue failed to retrieve unprocessed queue items.", ex);
                            break;
                        }
                    }
                }

                _log.Debug("End dequeue unprocessed items");
            }
        }

        private FeedModel GetUnprocessedFeed(IEnumerable<IndexRequestQueueItem> queueItems)
        {
            var feed = new FeedModel
            {
                Id = RequestFeedId,
                LastUpdatedTime = _timeProvider?.UtcNow ?? DateTime.UtcNow
            };

            // Add client version as an attribute extension to this feed
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            feed.AttributeExtensions.Add(
                _options.SyndicationItemAttributeNameVersion,
                $"EPiServer.Search v.{version.Major}.{version.Minor}.{version.Build}.{version.Revision}");

            feed.Items = queueItems.Select(ConstructSyndicationItem);

            return feed;
        }

        private FeedItemModel ConstructSyndicationItem(IndexRequestQueueItem queueItem)
        {
            FeedItemModel item = JsonSerializer.Deserialize<FeedItemModel>(queueItem.FeedItemJson);

            return item;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                ProcessQueue();
            }
            finally
            {
                _queueFlushTimer.Enabled = true;
            }
        }
    }
}
