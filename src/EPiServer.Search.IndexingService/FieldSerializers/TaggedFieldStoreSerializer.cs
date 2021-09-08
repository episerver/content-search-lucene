using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Xml.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal class TaggedFieldStoreSerializer : IndexFieldStoreSerializerBase
    {
        internal TaggedFieldStoreSerializer(FeedItemModel feedItem)
            : base(feedItem)
        {
        }

        internal TaggedFieldStoreSerializer(string fieldStoreValue)
            : base(fieldStoreValue)
        {
        }

        // Add prefix and suffix to ensure that categories with white spaces always stick together 
        // in searches and to get them back in their original shape.
        internal virtual string ToFieldStoreString(string syndicationItemElementExtensionName)
        {
            string value = base.ToFieldStoreValue();

            if (FeedItem != null)
            {
                StringBuilder acl = new StringBuilder();

                var element = TryParseCollection(FeedItem.ElementExtensions[syndicationItemElementExtensionName]);

                if (element != null)
                {
                    foreach (string e in element)
                    {
                        acl.Append(IndexingServiceSettings.TagsPrefix);
                        acl.Append(e.Trim());
                        acl.Append(IndexingServiceSettings.TagsSuffix);
                        acl.Append(" ");
                    }

                    value = acl.ToString().Trim();
                }
                
            }
            return value;
        }

        internal void AddFieldStoreValueToSyndicationItem(FeedItemModel feedItem, string syndicationItemElementExtensionName)
        {
            if (!String.IsNullOrEmpty(FieldStoreValue))
            {
                MatchCollection matches = SplitFieldStoreValue();
                Collection<string> element = new Collection<string>();
                foreach (Match match in matches)
                {
                    if (match.Value != null)
                    {
                        string value = GetOriginalValue(match.Value);
                        element.Add(value);
                    }
                }
                feedItem.ElementExtensions[syndicationItemElementExtensionName] = element;
            }
            else
            {
                base.AddFieldStoreValueToSyndicationItem(feedItem);
            }
        }

        protected MatchCollection SplitFieldStoreValue()
        {
            return Regex.Matches(FieldStoreValue, "\\[\\[.*?\\]\\]");
        }

        protected string GetOriginalValue(string storedValue)
        {
            return storedValue.Replace(IndexingServiceSettings.TagsPrefix, "").Replace(IndexingServiceSettings.TagsSuffix, "");
        }
        private Collection<string> TryParseCollection(object o)
        {
            Collection<string> c = new Collection<string>();
            if (o is JsonElement)
            {
                var json = ((JsonElement)o).GetRawText();
                c = JsonSerializer.Deserialize<Collection<string>>(json);
            }
            else if (o is Collection<string>)
            {
                c = o as Collection<string>;
            }
            return c;
        }
    }
}