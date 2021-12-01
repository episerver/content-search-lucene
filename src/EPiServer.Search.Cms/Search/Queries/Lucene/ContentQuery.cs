using EPiServer.Core;

namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// Representing a query to the Lucene Indexing Service for an <see cref="EPiServer.Core.IContent"/> item.
    /// </summary>
    public class ContentQuery : ContentQuery<IContent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentQuery"/> class.
        /// </summary>
        public ContentQuery()
            : base() { }
    }

    /// <summary>
    /// Representing a query to the Lucene Indexing Service for a content item.
    /// </summary>
    /// <typeparam name="T">The type of the content to search for.</typeparam>
    /// <example>
    /// <para>
    /// Example of how you could search for pages.
    /// </para>
    /// <code source="../CodeSamples/EPiServer/Search/SearchQuerySamples.cs" region="PageSearch" lang="cs" />
    /// </example>
    public class ContentQuery<T> : IQueryExpression
        where T : IContentData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentQuery&lt;T&gt;"/> class.
        /// </summary>
        public ContentQuery() { }

        /// <summary>
        /// Gets the query expression for this <see cref="ContentQuery&lt;T&gt;"/> instance.
        /// </summary>
        /// <returns>A query expression string.</returns>
        public virtual string GetQueryExpression() => new FieldQuery("\"" + ContentSearchHandler.GetItemTypeSection<T>() + "\"", Field.ItemType).GetQueryExpression();
    }

}
