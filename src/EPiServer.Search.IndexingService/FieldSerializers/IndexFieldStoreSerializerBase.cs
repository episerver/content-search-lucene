namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal abstract class IndexFieldStoreSerializerBase
    {
        internal IndexFieldStoreSerializerBase(FeedItemModel feedItem)
        {
            FeedItem = feedItem;
        }

        internal IndexFieldStoreSerializerBase(string fieldStoreValue)
        {
            FieldStoreValue = fieldStoreValue;
        }

        internal virtual string ToFieldStoreValue() => (FieldStoreValue != null) ? FieldStoreValue : "";

        internal virtual void AddFieldStoreValueToSyndicationItem(FeedItemModel feedItem)
        {
            //No default implementation
        }

        internal string FieldStoreValue
        {
            get;
            set;
        }

        internal FeedItemModel FeedItem
        {
            get;
            set;
        }
    }
}
