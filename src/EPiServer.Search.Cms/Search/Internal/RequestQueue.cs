using System.Collections.Generic;
using System.Linq;
using EPiServer.Data.Dynamic;
using EPiServer.Search.Data;
using Microsoft.Extensions.Options;

namespace EPiServer.Search.Internal
{
    public class RequestQueue
    {
        private const int TruncateBatchSize = 100;
        private readonly SearchOptions _options;

        public RequestQueue(IOptions<SearchOptions> options)
        {
            _options = options.Value;
        }

        public virtual void Add(IndexRequestQueueItem item)
        {
            if (Store() != null)
            {
                Store().Save(item);
            }
        }

        public virtual IEnumerable<IndexRequestQueueItem> Get(string namedIndexingService, int maxCount)
        {
            if (Store() != null)
            {
                var queueItems =
                (from queueItem in Store().Items<IndexRequestQueueItem>()
                 where queueItem.NamedIndexingService == namedIndexingService
                 orderby queueItem.Timestamp ascending
                 select queueItem).Take(maxCount);

                return queueItems.ToList();
            }
            else
            {
                return null;
            }
        }

        public virtual void Remove(IEnumerable<IndexRequestQueueItem> items)
        {
            if (Store() != null)
            {
                using (var dataStore = Store())
                {
                    foreach (var item in items)
                    {
                        dataStore.Delete(item.Id);
                    }
                }
            }
        }

        public virtual void Truncate()
        {
            if (Store() != null)
            {
                Store().DeleteAll();
            }
        }

        public virtual void Truncate(string namedIndexingService, string namedIndex)
        {
            if (Store() != null)
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

        }

        private DynamicDataStore Store() => DynamicDataStoreFactory.Instance.GetStore(_options.DynamicDataStoreName) ?? null;
    }
}
