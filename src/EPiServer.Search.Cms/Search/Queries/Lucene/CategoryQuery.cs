namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// Representing a CategoryQuery for the Lucene Indexing Service with correct field name and Lucene syntax.
    /// </summary>
    public class CategoryQuery : CollectionQueryBase
    {
        /// <summary>
        /// Constructing a <see cref="CategoryQuery"/> with an inner operator used between every category in Categories
        /// </summary>
        /// <param name="innerOperator"></param>
        public CategoryQuery(LuceneOperator innerOperator)
            : base(SearchSettings.Options.IndexingServiceFieldNameCategories, innerOperator)
        {
        }
    }
}
