namespace EPiServer.Search.IndexingService.Helpers
{
    public interface IFeedHelper
    {
        string PrepareAuthors(FeedItemModel item);
        void SetElementValue(FeedItemModel item, string elementExtensionName, string value);
        void SetAttributeValue(FeedItemModel item, string attributeExtensionName, string value);
        string GetAttributeValue(FeedItemModel item, string attributeName);
        string GetElementValue(FeedItemModel item, string elementName);
        bool GetAutoUpdateVirtualPathValue(FeedItemModel item);
    }
}
