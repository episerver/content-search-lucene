using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService.Helpers
{
    public class FeedHelper : IFeedHelper
    {
        public string PrepareAuthors(FeedItemModel item)
        {
            StringBuilder authors = new StringBuilder();
            if (item.Authors != null)
            {
                foreach (string person in item.Authors)
                {
                    authors.Append(person);
                    authors.Append(" ");
                }
            }
            return authors.ToString().Trim();
        }

        public void SetElementValue(FeedItemModel item, string elementExtensionName, string value)
        {
            item.ElementExtensions[elementExtensionName] = value;
        }

        public void SetAttributeValue(FeedItemModel item, string attributeExtensionName, string value)
        {
            item.AttributeExtensions[attributeExtensionName] = value;
        }

        public string GetAttributeValue(FeedItemModel item, string attributeName)
        {
            string value = String.Empty;
            if (item.AttributeExtensions.ContainsKey(attributeName))
            {
                value = item.AttributeExtensions[attributeName];
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                value = String.Empty;
            }
            return value;
        }

        public string GetElementValue(FeedItemModel item, string elementName)
        {
            string value = "";
            if (item.ElementExtensions.ContainsKey(elementName))
            {
                value = item.ElementExtensions[elementName].ToString();
            }
            return value;
        }

        public bool GetAutoUpdateVirtualPathValue(FeedItemModel item)
        {
            bool autoUpdateVirtualPath;
            if (bool.TryParse(GetAttributeValue(item, IndexingServiceSettings.SyndicationItemAttributeNameAutoUpdateVirtualPath), out autoUpdateVirtualPath))
            {
                return autoUpdateVirtualPath;
            }
            return false;
        }
    }
}
