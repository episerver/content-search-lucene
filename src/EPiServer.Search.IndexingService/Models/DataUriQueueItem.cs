using EPiServer.Search.IndexingService.Helpers;

namespace EPiServer.Search.IndexingService.Models
{
    public class DataUriQueueItem
    {
        private readonly FeedItemModel _item;
        private readonly NamedIndex _namedIndex;
        private readonly ILuceneHelper _luceneHelper;

        public DataUriQueueItem(FeedItemModel item, NamedIndex namedIndex, ILuceneHelper luceneHelper)
        {
            this._item = item;
            this._namedIndex = namedIndex;
            this._luceneHelper = luceneHelper;
        }

        internal void Do() => _luceneHelper.HandleDataUri(_item, _namedIndex);
    }
}
