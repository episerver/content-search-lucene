namespace EPiServer.Search
{
    /// <summary>
    /// Defines an interface for performing Reindexing of items
    /// </summary>
    public interface IReIndexable
    {

        /// <summary>
        /// Re-Index.
        /// </summary>
        void ReIndex();

        /// <summary>
        /// Gets the NamedIndex.
        /// </summary>
        string NamedIndex { get; }

        /// <summary>
        /// Gets the named indexing service.
        /// </summary>
        string NamedIndexingService { get; }
    }
}
