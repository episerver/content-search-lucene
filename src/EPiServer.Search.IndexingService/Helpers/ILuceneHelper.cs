using Lucene.Net.Documents;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService.Helpers
{
    public interface ILuceneHelper
    {
        Document GetDocumentFromSyndicationItem(FeedItemModel feedItem, NamedIndex namedIndex);
        void AddAllSearchableContentsFieldToDocument(Document doc, NamedIndex namedIndex);
        string GetReferenceData(string referenceId, NamedIndex namedIndex);
        bool UpdateReference(string referenceId, string itemId, NamedIndex mainNamedIndex);
        bool WriteToIndex(string itemId, Document doc, NamedIndex namedIndex);
        bool Add(FeedItemModel item, NamedIndex namedIndex);
        void Remove(FeedItemModel feedItem, NamedIndex namedIndex);
        void Remove(string itemId, NamedIndex namedIndex, bool removeRef);
        bool RemoveByVirtualPath(string virtualPath);
        Collection<ScoreDocument> GetScoreDocuments(string q, bool excludeNotPublished, Collection<NamedIndex> namedIndexes, int offset, int limit, int maxHits, out int totalHits);
        bool DeleteFromIndex(NamedIndex namedIndex, string itemId, bool deleteRef);
        void Update(FeedItemModel feedItem, NamedIndex namedIndex);
        bool UpdateVirtualPaths(string oldVirtualPath, string newVirtualPath);
        string GetReferenceIdForItem(string itemId, NamedIndex namedIndex);
        bool HandleDataUri(FeedItemModel item, NamedIndex namedIndex);
    }
}
