namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// Filds available for separate free text search in the Indexing Service
    /// </summary>
    public enum Field
    {
        /// <summary>
        /// The default field contains a aggregation of searchable fields
        /// </summary>
        Default = 0,
        /// <summary>
        /// The unique ídentifier for an item within a named index
        /// </summary>
        Id = 1,
        /// <summary>
        /// The title of the item
        /// </summary>
        Title = 2,
        /// <summary>
        /// The display text of the item. Typically contains text that should be displayed together with search results
        /// </summary>
        DisplayText = 3,
        /// <summary>
        /// The authors of the item content
        /// </summary>
        Authors = 4,
        /// <summary>
        /// Date when the item was created
        /// </summary>
        Created = 5,
        /// <summary>
        /// Date when the item was last modified
        /// </summary>
        Modified = 6,
        /// <summary>
        /// The culture for the item. Typically language-region code
        /// </summary>
        Culture = 7,
        /// <summary>
        /// The Type of the item. Typically Class and Assembly name
        /// </summary>
        ItemType = 8,
        /// <summary>
        /// The Status of the item. Containing one value of <see cref="ItemStatus"/>
        /// </summary>
        ItemStatus = 9
    }
}
