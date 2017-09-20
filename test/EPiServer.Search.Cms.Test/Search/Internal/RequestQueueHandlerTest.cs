using EPiServer.Framework;
using EPiServer.Search.Configuration;
using EPiServer.Search.Data;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using Xunit;

namespace EPiServer.Search.Internal
{
    public class RequestQueueHandlerTest
    {
        private static readonly string DefaultIndexingService = "default";
        private SearchOptions _defaultOptions;

        public RequestQueueHandlerTest()
        {
            _defaultOptions = new SearchOptions
            {
                DefaultIndexingServiceName = DefaultIndexingService,
                IndexingServiceReferences = { new IndexingServiceReference { Name = DefaultIndexingService } },
            };
        }

        [Fact]
        public void ProcessQueue_WhenThereAreNoItemsInQueue_ShouldNotSendAnyRequests()
        {
            var requestHandler = new Mock<RequestHandler>(null);

            Handler(requestHandler.Object).ProcessQueue();

            requestHandler.Verify(x => x.SendRequest(It.IsAny<SyndicationFeed>(), It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public void ProcessQueue_WhenThereAreItemsInQueue_ShouldSendRequest()
        {
            var requestHandler = new Mock<RequestHandler>(null);

            var queue = new TestQueue { QueueItem("A"), QueueItem("B"), QueueItem("C"), };

            Handler(requestHandler.Object, queue).ProcessQueue();

            requestHandler.Verify(x => x.SendRequest(It.IsAny<SyndicationFeed>(), DefaultIndexingService), Times.Once());
        }

        [Fact]
        public void ProcessQueue_WhenThereItemsForMultipleServicesInQueue_ShouldSendOneRequestForEachService()
        {
            const string SecondIndexingService = "second";
            var options = new SearchOptions
            {
                IndexingServiceReferences =
                {
                    new IndexingServiceReference { Name = DefaultIndexingService },
                    new IndexingServiceReference { Name = SecondIndexingService },
                }
            };
            var requestHandler = new Mock<RequestHandler>(null);

            var queue = new TestQueue { QueueItem("A"), QueueItem("B", SecondIndexingService), QueueItem("C"), };

            Handler(requestHandler.Object, queue, options).ProcessQueue();

            requestHandler.Verify(x => x.SendRequest(It.IsAny<SyndicationFeed>(), DefaultIndexingService), Times.Once());
            requestHandler.Verify(x => x.SendRequest(It.IsAny<SyndicationFeed>(), SecondIndexingService), Times.Once());
        }

        [Fact]
        public void ProcessQueue_WhenOneOfMultipleServicesThrowsOnSend_ShouldKeepSendingRequestsForOtherServices()
        {
            const string SecondIndexingService = "second";
            var options = new SearchOptions
            {
                IndexingServiceReferences =
                {
                    new IndexingServiceReference { Name = DefaultIndexingService },
                    new IndexingServiceReference { Name = SecondIndexingService },
                }
            };

            var requestHandler = new Mock<RequestHandler>(null);
            requestHandler.Setup(x => x.SendRequest(It.IsAny<SyndicationFeed>(), DefaultIndexingService)).Throws<InvalidOperationException>();

            var queue = new TestQueue { QueueItem("A"), QueueItem("B", SecondIndexingService), QueueItem("C"), };

            Handler(requestHandler.Object, queue, options).ProcessQueue();

            requestHandler.Verify(x => x.SendRequest(It.IsAny<SyndicationFeed>(), SecondIndexingService), Times.Once());
        }

        [Fact]
        public void ProcessQueue_WhenQueuedItemsHasBeenSendSuccessfully_ShouldRemoveItems()
        {
            var requestHandler = new Mock<RequestHandler>(null);
            requestHandler.Setup(x => x.SendRequest(It.IsAny<SyndicationFeed>(), DefaultIndexingService)).Returns(true);

            var queue = new TestQueue { QueueItem("A"), QueueItem("B"), QueueItem("C"), };

            Handler(requestHandler.Object, queue).ProcessQueue();

            Assert.Empty(queue.Items);
        }

        [Fact]
        public void ProcessQueue_WhenSendingQueuedItemsWasUnsuccessfull_ShouldKeepItemsInQueue()
        {
            var requestHandler = new Mock<RequestHandler>(null);
            requestHandler.Setup(x => x.SendRequest(It.IsAny<SyndicationFeed>(), DefaultIndexingService)).Returns(false);

            var queue = new TestQueue { QueueItem("A"), QueueItem("B"), QueueItem("C"), };

            Handler(requestHandler.Object, queue).ProcessQueue();

            Assert.Equal(3, queue.Items.Count);
        }

        [Fact]
        public void ProcessQueue_WhenSendingQueuedItemsWasUnsuccessfullForOneService_ShouldKeepItemsInQueueForFailingService()
        {
            const string SecondIndexingService = "second";
            var options = new SearchOptions
            {
                IndexingServiceReferences =
                {
                    new IndexingServiceReference { Name = DefaultIndexingService },
                    new IndexingServiceReference { Name = SecondIndexingService },
                }
            };

            var requestHandler = new Mock<RequestHandler>(null);
            requestHandler.Setup(x => x.SendRequest(It.IsAny<SyndicationFeed>(), DefaultIndexingService)).Returns(true);
            requestHandler.Setup(x => x.SendRequest(It.IsAny<SyndicationFeed>(), SecondIndexingService)).Returns(false);

            var queue = new TestQueue { QueueItem("A"), QueueItem("B", SecondIndexingService), QueueItem("C"), };

            Handler(requestHandler.Object, queue, options).ProcessQueue();

            Assert.All(queue.Items, x => Assert.Equal(SecondIndexingService, x.NamedIndexingService));
        }

        private RequestQueueHandler Handler(RequestHandler requestHandler = null, RequestQueue queue = null, SearchOptions options = null, ITimeProvider timeProvider = null)
        {
            return new RequestQueueHandler(
                requestHandler ?? new Mock<RequestHandler>(options ?? _defaultOptions).Object,
                queue ?? new Mock<RequestQueue>(options ?? _defaultOptions).Object,
                options ?? _defaultOptions,
                timeProvider);
        }

        private IndexRequestQueueItem QueueItem(string id, string service = null)
        {
            return QueueItem(new IndexRequestItem(id, IndexAction.None), service);
        }

        private IndexRequestQueueItem QueueItem(IndexRequestItem item, string service = null)
        {
            return new IndexRequestQueueItem
            {
                IndexItemId = item.Id,
                NamedIndex = item.NamedIndex,
                NamedIndexingService = service ?? DefaultIndexingService,
                SyndicationItemXml = item.ToSyndicationItemXml(_defaultOptions),
                Timestamp = DateTime.Now
            };
        }

        private class TestQueue : RequestQueue, IEnumerable<IndexRequestQueueItem>
        {
            public TestQueue() : base(new SearchOptions()) { }

            public List<IndexRequestQueueItem> Items { get; } = new List<IndexRequestQueueItem>();

            public override void Add(IndexRequestQueueItem item)
            {
                Items.Add(item);
            }

            public override IEnumerable<IndexRequestQueueItem> Get(string namedIndexingService, int maxCount)
            {
                return Items
                    .Where(x => x.NamedIndexingService == namedIndexingService)
                    .OrderBy(x => x.Timestamp)
                    .Take(maxCount)
                    .ToList();
            }

            public override void Remove(IEnumerable<IndexRequestQueueItem> items)
            {
                foreach (var item in items)
                {
                    Items.Remove(item);
                }
            }

            public override void Truncate()
            {
                Items.Clear();
            }

            public override void Truncate(string namedIndexingService, string namedIndex)
            {
                Items.RemoveAll(x => x.NamedIndexingService == namedIndexingService && x.NamedIndex == namedIndex);
            }

            public IEnumerator<IndexRequestQueueItem> GetEnumerator() => ((IEnumerable<IndexRequestQueueItem>)Items).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class TestRequestHandler : RequestHandler
        {
            public TestRequestHandler(SearchOptions options)
                : base(options) { }

            public List<RequestInfo> Requests { get; } = new List<RequestInfo>();

            internal override void MakeHttpRequest(string url, string method, IndexingServiceReference indexingServiceReference, Stream postData = null, Action<Stream> responseStreamHandler = null)
            {
                var requestInfo = new RequestInfo
                {
                    Url = url,
                    Method = method,
                    IndexingServiceReference = indexingServiceReference
                };

                if (postData != null)
                {
                    using (var r = new StreamReader(postData))
                    {
                        requestInfo.PostData = r.ReadToEnd();
                    }
                }

                Requests.Add(requestInfo);
            }
        }

        private class RequestInfo
        {
            public string Url { get; set; }
            public string Method { get; set; }
            public IndexingServiceReference IndexingServiceReference { get; set; }
            public string PostData { get; set; }
        }
    }
}
