using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lucene.Net.Documents;

namespace EPiServer.Search.IndexingService
{
    internal class FieldProperties
    {
        internal Field.Store FieldStore
        {
            get;
            set;
        }

        internal Field.Index FieldIndex
        {
            get;
            set;
        }
    }
}
