using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel.Syndication;

namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal abstract class IndexFieldStoreSerializerBase
    {
        internal IndexFieldStoreSerializerBase(SyndicationItem syndicationItem)
        {
            SyndicationItem = syndicationItem;
        }

        internal IndexFieldStoreSerializerBase(string fieldStoreValue)
        {
            FieldStoreValue = fieldStoreValue;
        }

        internal virtual string ToFieldStoreValue()
        {

            return (FieldStoreValue != null) ? FieldStoreValue : "";
        }

        internal virtual void AddFieldStoreValueToSyndicationItem(SyndicationItem syndicationItem)
        {
            //No default implementation
        }

        internal string FieldStoreValue
        {
            get;
            set;
        }

        internal SyndicationItem SyndicationItem
        {
            get;
            set;
        }
    }
}
