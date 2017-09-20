using System.Collections.Generic;
using System.Linq;
using EPiServer.Data.Dynamic;
using EPiServer.Search.Data;

namespace EPiServer.Search.Internal
{
    public class RequestQueue
    {
        private const int TruncateBatchSize = 100;
        private readonly SearchOptions _options;

        public RequestQueue(SearchOptions options)
        {
            _options = options;
        }

        public virtual void Add(IndexRequestQueueItem item)
        {
            Store().Save(item);
        }

        public virtual IEnumerable<IndexRequestQueueItem> Get(string namedIndexingService, int maxCount)
        {
            var queueItems =
                (from queueItem in Store().Items<IndexRequestQueueItem>()
                 where queueItem.NamedIndexingService == namedIndexingService
                 orderby queueItem.Timestamp ascending
                 select queueItem).Take(maxCount);

            return queueItems.ToList();
        }

        public virtual void Remove(IEnumerable<IndexRequestQueueItem> items)
        {
            using (var dataStore = Store())
            {
                foreach (var item in items)
                {
                    dataStore.Delete(item.Id);
                }
            }
        }

        public virtual void Truncate()
        {
            Store().DeleteAll();
        }

        public virtual void Truncate(string namedIndexingService, string namedIndex)
        {
            List<IndexRequestQueueItem> queueItems = null;
            do
            {
                queueItems = (from queueItem in Store().Items<IndexRequestQueueItem>()
                              where queueItem.NamedIndexingService == namedIndexingService && queueItem.NamedIndex == namedIndex
                              select queueItem)
                              .Take(TruncateBatchSize)
                              .ToList();

                foreach (var queueItem in queueItems)
                {
                    Store().Delete(queueItem.Id);
                }
            } while (queueItems.Count > 0);

        }

        private DynamicDataStore Store()
        {
            var dataStore = DynamicDataStoreFactory.Instance.GetStore(_options.DynamicDataStoreName) ??
                DynamicDataStoreFactory.Instance.CreateStore(_options.DynamicDataStoreName, typeof(IndexRequestQueueItem));

            return dataStore;
        }
    }
}
