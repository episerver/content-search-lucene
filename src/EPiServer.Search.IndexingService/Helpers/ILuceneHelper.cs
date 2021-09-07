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
        void UpdateReference(string referenceId, string itemId, NamedIndex mainNamedIndex);
        Document GetDocumentById(string id, NamedIndex namedIndex);
        void WriteToIndex(string itemId, Document doc, NamedIndex namedIndex);
        void Add(FeedItemModel item, NamedIndex namedIndex);
        bool DocumentExists(string itemId, NamedIndex namedIndex);
        Collection<ScoreDocument> SingleIndexSearch(string q, NamedIndex namedIndex, int maxHits, out int totalHits);
        void Remove(FeedItemModel feedItem, NamedIndex namedIndex);
        void Remove(string itemId, NamedIndex namedIndex, bool removeRef);
        void RemoveByVirtualPath(string virtualPath);
        Collection<ScoreDocument> GetScoreDocuments(string q, bool excludeNotPublished, Collection<NamedIndex> namedIndexes, int offset, int limit, int maxHits, out int totalHits);
        Collection<ScoreDocument> MultiIndexSearch(string q, Collection<NamedIndex> namedIndexes, int maxHits, out int totalHits);
        void OptimizeIndex(NamedIndex namedIndex);
        bool DeleteFromIndex(NamedIndex namedIndex, string itemId, bool deleteRef);
        void Update(FeedItemModel feedItem, NamedIndex namedIndex);
        void UpdateVirtualPaths(string oldVirtualPath, string newVirtualPath);
        FeedItemModel GetSyndicationItemFromDocument(ScoreDocument scoreDocument);
        Lucene.Net.Store.Directory CreateIndex(string name, DirectoryInfo directoryInfo);
        string GetReferenceIdForItem(string itemId, NamedIndex namedIndex);
        void HandleDataUri(FeedItemModel item, NamedIndex namedIndex);
    }
}
