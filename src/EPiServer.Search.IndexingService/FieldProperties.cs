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

#pragma warning disable CS0618 // Type or member is obsolete
        internal Field.Index FieldIndex
#pragma warning restore CS0618 // Type or member is obsolete
        {
            get;
            set;
        }
    }
}
