namespace EPiServer.Search
{
    /// <summary>
    /// Resets all indexes and then performs re-indexing by calling into all registered <see cref="IReIndexable"/>
    /// </summary>
    public interface IReIndexManager
    {
        /// <summary>
        /// Run re-indexing, 
        /// </summary>
        void ReIndex();
    }
}