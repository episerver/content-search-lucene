using System.Linq;
using System.Text;

namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal class AuthorsFieldStoreSerializer : PipeSeparatedFieldStoreSerializer
    {
        internal AuthorsFieldStoreSerializer(FeedItemModel feedItem)
            : base(feedItem)
        {
        }

        internal AuthorsFieldStoreSerializer(string indexFieldStoreValue)
            : base(indexFieldStoreValue)
        {
        }

        internal override string ToFieldStoreValue()
        {
            if (FeedItem != null)
            {
                var authors = new StringBuilder();

                foreach (var person in FeedItem.Authors.Where(a => !string.IsNullOrEmpty(a)))
                {
                    authors.Append(person.Trim());
                    authors.Append("|");
                }

                if (authors.Length > 0)
                {
                    authors.Remove(authors.Length - 1, 1);
                }

                return authors.ToString().Trim();
            }
            else
            {
                return base.ToFieldStoreValue();
            }
        }

        internal override void AddFieldStoreValueToSyndicationItem(FeedItemModel feedItem)
        {
            var nodes = base.SplitFieldStoreValue();
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node))
                {
                    feedItem.Authors.Add(node);
                }
            }
        }
    }
}
