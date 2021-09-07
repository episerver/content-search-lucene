using EPiServer.Search.IndexingService.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService.Models
{
    public class DataUriQueueItem
    {
        private FeedItemModel _item;
        private NamedIndex _namedIndex;
        private ILuceneHelper _luceneHelper;

        public DataUriQueueItem(FeedItemModel item, NamedIndex namedIndex, ILuceneHelper luceneHelper)
        {
            this._item = item;
            this._namedIndex = namedIndex;
            this._luceneHelper = luceneHelper;
        }

        internal void Do()
        {
            _luceneHelper.HandleDataUri(_item, _namedIndex);
        }
    }
}
