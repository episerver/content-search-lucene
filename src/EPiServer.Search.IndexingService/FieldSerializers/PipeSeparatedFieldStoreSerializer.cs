using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Xml.Linq;
using System.ServiceModel.Syndication;

namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal class PipeSeparatedFieldStoreSerializer : IndexFieldStoreSerializerBase
    {
        public PipeSeparatedFieldStoreSerializer(SyndicationItem syndicationItem)
            : base(syndicationItem)
        {
        }

        internal PipeSeparatedFieldStoreSerializer(string fieldStoreValue)
            : base(fieldStoreValue)
        {
        }

        internal string ToFieldStoreValue(string syndicationItemElementExtensionName)
        {
            if (SyndicationItem != null)
            {
                StringBuilder sb = new StringBuilder();

                XElement element = SyndicationItem.ElementExtensions.ReadElementExtensions<XElement>
                    (syndicationItemElementExtensionName,
                    IndexingServiceSettings.XmlQualifiedNamespace).ElementAt<XElement>(0);

                foreach (XElement e in element.Elements())
                {
                    sb.Append(e.Value);
                    sb.Append("|");
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

        internal void AddFieldStoreValueToSyndicationItem(SyndicationItem syndicationItem, string syndicationItemElementExtensionName)
        {
            if (!String.IsNullOrEmpty(FieldStoreValue))
            {
                XNamespace ns = IndexingServiceSettings.XmlQualifiedNamespace;
                char[] delimiter = { '|' };
                string[] nodes = SplitFieldStoreValue();
                XElement element = new XElement(ns + syndicationItemElementExtensionName);
                foreach (string node in nodes)
                {
                    element.Add(new XElement(ns + "Item", node));
                }
                syndicationItem.ElementExtensions.Add(element.CreateReader());
            }
            else
            {
                base.AddFieldStoreValueToSyndicationItem(syndicationItem);
            }
        }

        protected string[] SplitFieldStoreValue()
        {
            char[] delimiter = { '|' };
            return FieldStoreValue.Split(delimiter);
        }
    }
}
