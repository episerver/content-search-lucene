using System.Collections.ObjectModel;
using System.IO;
using Lucene.Net.Documents;

namespace EPiServer.Search.IndexingService.Helpers
{
    public interface IDocumentHelper
    {
        Collection<ScoreDocument> SingleIndexSearch(string q, NamedIndex namedIndex, int maxHits, out int totalHits);
        void OptimizeIndex(NamedIndex namedIndex);
        Collection<ScoreDocument> MultiIndexSearch(string q, Collection<NamedIndex> namedIndexes, int maxHits, out int totalHits);
        FeedItemModel GetSyndicationItemFromDocument(ScoreDocument scoreDocument);
        Lucene.Net.Store.Directory CreateIndex(string name, DirectoryInfo directoryInfo);
        bool DocumentExists(string itemId, NamedIndex namedIndex);
        Document GetDocumentById(string id, NamedIndex namedIndex);
    }
}
