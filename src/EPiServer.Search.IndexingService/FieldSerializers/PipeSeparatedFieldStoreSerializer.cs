using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal class PipeSeparatedFieldStoreSerializer : IndexFieldStoreSerializerBase
    {
        public PipeSeparatedFieldStoreSerializer(FeedItemModel feedItem)
            : base(feedItem)
        {
        }

        internal PipeSeparatedFieldStoreSerializer(string fieldStoreValue)
            : base(fieldStoreValue)
        {
        }

        internal string ToFieldStoreValue(string syndicationItemElementExtensionName)
        {
            if (FeedItem != null)
            {
                var sb = new StringBuilder();

                var element = TryParseCollection(FeedItem.ElementExtensions[syndicationItemElementExtensionName]);

                if (element != null)
                {
                    foreach (var e in element)
                    {
                        sb.Append(e);
                        sb.Append("|");
                    }
                }

                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                }

                return sb.ToString().Trim();
            }
            else
            {
                return base.ToFieldStoreValue();
            }
        }

        internal void AddFieldStoreValueToSyndicationItem(FeedItemModel feedItem, string syndicationItemElementExtensionName)
        {
            if (!string.IsNullOrEmpty(FieldStoreValue))
            {
                var element = new Collection<string>();

                var nodes = SplitFieldStoreValue();

                foreach (var node in nodes)
                {
                    element.Add(node);
                }
                feedItem.ElementExtensions[syndicationItemElementExtensionName] = element;
            }
            else
            {
                base.AddFieldStoreValueToSyndicationItem(feedItem);
            }
        }

        protected string[] SplitFieldStoreValue()
        {
            char[] delimiter = { '|' };
            return FieldStoreValue.Split(delimiter);
        }
        private Collection<string> TryParseCollection(object o)
        {
            var c = new Collection<string>();
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