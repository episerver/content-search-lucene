namespace EPiServer.Search.IndexingService
{
    public interface IIndexingServiceHandler
    {
        void ResetNamedIndex(string namedIndexName);

        void UpdateIndex(FeedModel feed);

        FeedModel GetNamedIndexes();

        FeedModel GetSearchResults(string q, string[] namedIndexNames, int offset, int limit);

        FeedModel GetSearchResults(string q, string namedIndexes, int offset, int limit);
    }
}
