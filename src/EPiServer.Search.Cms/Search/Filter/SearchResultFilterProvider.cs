namespace EPiServer.Search.Filter
{
    /// <summary>
    /// Abstract class for SearchResultFilterProvider
    /// </summary>
    public abstract class SearchResultFilterProvider
    {
        public abstract SearchResultFilter Filter(IndexResponseItem item);
    }
}
