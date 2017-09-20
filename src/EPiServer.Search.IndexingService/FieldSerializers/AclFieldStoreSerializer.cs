using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel.Syndication;
using System.Xml.Linq;
using System.Text;

namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal class AclFieldStoreSerializer : TaggedFieldStoreSerializer
    {
        internal AclFieldStoreSerializer(SyndicationItem syndicationItem)
            : base(syndicationItem)
        {
        }

        internal AclFieldStoreSerializer(string indexFieldStoreValue)
            : base(indexFieldStoreValue)
        {
        }

        internal override string ToFieldStoreValue()
        {
            return base.ToFieldStoreString(IndexingServiceSettings.SyndicationItemElementNameAcl);
        }

        internal override void AddFieldStoreValueToSyndicationItem(SyndicationItem syndicationItem)
        {
            base.AddFieldStoreValueToSyndicationItem(syndicationItem, IndexingServiceSettings.SyndicationItemElementNameAcl);
        }
    }
}
