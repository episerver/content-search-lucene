using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Xml.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal class TaggedFieldStoreSerializer : IndexFieldStoreSerializerBase
    {
        internal TaggedFieldStoreSerializer(SyndicationItem syndicationItem)
            : base(syndicationItem)
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

            if (SyndicationItem != null)
            {
                StringBuilder acl = new StringBuilder();

                Collection<XElement> elements = SyndicationItem.ElementExtensions.ReadElementExtensions<XElement>
                    (syndicationItemElementExtensionName,
                    IndexingServiceSettings.XmlQualifiedNamespace);

                if (elements.Count > 0)
                {
                    XElement element = elements.ElementAt<XElement>(0);
                    foreach (XElement e in element.Elements())
                    {
                        acl.Append(IndexingServiceSettings.TagsPrefix);
                        acl.Append(e.Value.Trim());
                        acl.Append(IndexingServiceSettings.TagsSuffix);
                        acl.Append(" ");
                    }

                    value = acl.ToString().Trim();
                }
                
            }
            return value;
        }

        internal void AddFieldStoreValueToSyndicationItem(SyndicationItem syndicationItem, string syndicationItemElementExtensionName)
        {
            if (!String.IsNullOrEmpty(FieldStoreValue))
            {
                XNamespace ns = IndexingServiceSettings.XmlQualifiedNamespace;
                MatchCollection matches = SplitFieldStoreValue();
                XElement element = new XElement(ns + syndicationItemElementExtensionName);
                foreach (Match match in matches)
                {
                    if (match.Value != null)
                    {
                        string value = GetOriginalValue(match.Value);
                        element.Add(new XElement(ns + "Item", value));
                    }
                }
                syndicationItem.ElementExtensions.Add(element.CreateReader());
            }
            else
            {
                base.AddFieldStoreValueToSyndicationItem(syndicationItem);
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
    }
}
