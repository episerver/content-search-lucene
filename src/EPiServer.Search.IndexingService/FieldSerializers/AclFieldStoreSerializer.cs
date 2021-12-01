namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal class AclFieldStoreSerializer : TaggedFieldStoreSerializer
    {
        internal AclFieldStoreSerializer(FeedItemModel feedItem)
            : base(feedItem)
        {
        }

        internal AclFieldStoreSerializer(string indexFieldStoreValue)
            : base(indexFieldStoreValue)
        {
        }

        internal override string ToFieldStoreValue() => base.ToFieldStoreString(IndexingServiceSettings.SyndicationItemElementNameAcl);

        internal override void AddFieldStoreValueToSyndicationItem(FeedItemModel feedItem) => base.AddFieldStoreValueToSyndicationItem(feedItem, IndexingServiceSettings.SyndicationItemElementNameAcl);
    }
}
