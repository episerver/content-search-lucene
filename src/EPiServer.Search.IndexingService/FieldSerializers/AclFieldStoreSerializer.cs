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
        internal AclFieldStoreSerializer(FeedItemModel feedItem)
            : base(feedItem)
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

        internal override void AddFieldStoreValueToSyndicationItem(FeedItemModel feedItem)
        {
            base.AddFieldStoreValueToSyndicationItem(feedItem, IndexingServiceSettings.SyndicationItemElementNameAcl);
        }
    }
}
