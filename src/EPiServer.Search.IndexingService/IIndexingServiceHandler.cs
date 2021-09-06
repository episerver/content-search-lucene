using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService
{
    public interface IIndexingServiceHandler
    {
        void HandleDataUri(FeedItemModel item, NamedIndex namedIndex);

        Lucene.Net.Store.Directory CreateIndex(string name, DirectoryInfo directoryInfo);

        void ResetNamedIndex(string namedIndexName);

        void UpdateIndex(FeedModel feed);

        FeedModel GetNamedIndexes();

        FeedModel GetSearchResults(string q, string[] namedIndexNames, int offset, int limit);
    }
}
