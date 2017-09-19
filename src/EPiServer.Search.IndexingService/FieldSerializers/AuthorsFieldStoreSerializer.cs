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
        internal AuthorsFieldStoreSerializer(SyndicationItem syndicationItem)
            : base(syndicationItem)
        {
        }

        internal AuthorsFieldStoreSerializer(string indexFieldStoreValue)
            : base(indexFieldStoreValue)
        {
        }

        internal override string ToFieldStoreValue()
        {
            if (SyndicationItem != null)
            {
                StringBuilder authors = new StringBuilder();

                foreach (SyndicationPerson person in SyndicationItem.Authors.Where(a => a != null && !string.IsNullOrEmpty(a.Name)))
                {
                    authors.Append(person.Name.Trim());
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

        internal override void AddFieldStoreValueToSyndicationItem(SyndicationItem syndicationItem)
        {
            string[] nodes = base.SplitFieldStoreValue();
            foreach (string node in nodes)
            {
                if(!String.IsNullOrEmpty(node))
                {
                    syndicationItem.Authors.Add(new SyndicationPerson(String.Empty, node, String.Empty));
                }
            }
        }
    }
}
