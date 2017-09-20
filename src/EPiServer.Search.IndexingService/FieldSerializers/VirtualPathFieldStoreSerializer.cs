using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel.Syndication;
using System.Xml.Linq;
using System.Text;

namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal class VirtualPathFieldStoreSerializer : PipeSeparatedFieldStoreSerializer
    {
        internal VirtualPathFieldStoreSerializer(SyndicationItem syndicationItem)
            : base(syndicationItem)
        {
        }

        internal VirtualPathFieldStoreSerializer(string indexFieldStoreValue)
            : base(indexFieldStoreValue)
        {
        }

        internal override string ToFieldStoreValue()
        {
            return base.ToFieldStoreValue(IndexingServiceSettings.SyndicationItemElementNameVirtualPath);
        }

        internal override void AddFieldStoreValueToSyndicationItem(SyndicationItem syndicationItem)
        {
            base.AddFieldStoreValueToSyndicationItem(syndicationItem, IndexingServiceSettings.SyndicationItemElementNameVirtualPath);
        }
    }
}
