using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Xml.Linq;
using System.ServiceModel.Syndication;
using System.Collections.ObjectModel;

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
                StringBuilder sb = new StringBuilder();

                var element = FeedItem.ElementExtensions[syndicationItemElementExtensionName];

                if (element != null)
                {
                    foreach (string e in (Collection<string>)element)
                    {
                        sb.Append(e);
                        sb.Append("|");
                    }
                }

                if (sb.Length > 0)
                    sb.Remove(sb.Length - 1, 1);

                return sb.ToString().Trim();
            }
            else
            {
                return base.ToFieldStoreValue();
            }
        }

        internal void AddFieldStoreValueToSyndicationItem(FeedItemModel feedItem, string syndicationItemElementExtensionName)
        {
            if (!String.IsNullOrEmpty(FieldStoreValue))
            {
                Collection<string> element = new Collection<string>();

                string[] nodes = SplitFieldStoreValue();

                foreach (string node in nodes)
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
    }
}
