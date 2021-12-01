using System.Text;

namespace EPiServer.Search.IndexingService.Helpers
{
    public class FeedHelper : IFeedHelper
    {
        public string PrepareAuthors(FeedItemModel item)
        {
            var authors = new StringBuilder();
            if (item.Authors != null)
            {
                foreach (var person in item.Authors)
                {
                    authors.Append(person);
                    authors.Append(" ");
                }
            }
            return authors.ToString().Trim();
        }

        public void SetElementValue(FeedItemModel item, string elementExtensionName, string value) => item.ElementExtensions[elementExtensionName] = value;

        public void SetAttributeValue(FeedItemModel item, string attributeExtensionName, string value) => item.AttributeExtensions[attributeExtensionName] = value;

        public string GetAttributeValue(FeedItemModel item, string attributeName)
        {
            var value = string.Empty;
            if (item.AttributeExtensions.ContainsKey(attributeName))
            {
                value = item.AttributeExtensions[attributeName];
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                value = string.Empty;
            }
            return value;
        }

        public string GetElementValue(FeedItemModel item, string elementName)
        {
            var value = "";
            if (item.ElementExtensions.ContainsKey(elementName))
            {
                value = item.ElementExtensions[elementName].ToString();
            }
            return value;
        }

        public bool GetAutoUpdateVirtualPathValue(FeedItemModel item)
        {
            if (bool.TryParse(GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameAutoUpdateVirtualPath), out var autoUpdateVirtualPath))
            {
                return autoUpdateVirtualPath;
            }
            return false;
        }
    }
}
