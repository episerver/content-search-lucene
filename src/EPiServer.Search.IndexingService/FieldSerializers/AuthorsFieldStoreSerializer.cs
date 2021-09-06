using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel.Syndication;
using System.Xml.Linq;
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
                StringBuilder authors = new StringBuilder();

                foreach (string person in FeedItem.Authors.Where(a => !string.IsNullOrEmpty(a)))
                {
                    authors.Append(person.Trim());
                    authors.Append("|");
                }

                if (authors.Length > 0)
                    authors.Remove(authors.Length - 1, 1);

                return authors.ToString().Trim();
            }
            else
            {
                return base.ToFieldStoreValue();
            }
        }

        internal override void AddFieldStoreValueToSyndicationItem(FeedItemModel feedItem)
        {
            string[] nodes = base.SplitFieldStoreValue();
            foreach (string node in nodes)
            {
                if (!String.IsNullOrEmpty(node))
                {
                    feedItem.Authors.Add(node);
                }
            }
        }
    }
}
