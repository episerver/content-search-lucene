namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal class VirtualPathFieldStoreSerializer : PipeSeparatedFieldStoreSerializer
    {
        internal VirtualPathFieldStoreSerializer(FeedItemModel feedItem)
            : base(feedItem)
        {
        }

        internal VirtualPathFieldStoreSerializer(string indexFieldStoreValue)
            : base(indexFieldStoreValue)
        {
        }

        internal override string ToFieldStoreValue() => base.ToFieldStoreValue(IndexingServiceSettings.SyndicationItemElementNameVirtualPath);

        internal override void AddFieldStoreValueToSyndicationItem(FeedItemModel feedItem) => base.AddFieldStoreValueToSyndicationItem(feedItem, IndexingServiceSettings.SyndicationItemElementNameVirtualPath);
    }
}
