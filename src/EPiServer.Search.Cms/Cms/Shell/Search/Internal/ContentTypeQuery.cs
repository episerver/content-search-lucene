using System;
using EPiServer.Core;
using EPiServer.Search.Queries;
using EPiServer.Search.Queries.Lucene;

namespace EPiServer.Cms.Shell.Search.Internal
{
    public class ContentTypeQuery : IQueryExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentTypeQuery"/> class.
        /// </summary>
        public ContentTypeQuery(Type type)
        {
            Type = type;
        }

        public Type Type { get; private set; }

        /// <summary>
        /// Gets the query expression for this <see cref="ContentTypeQuery"/> instance.
        /// </summary>
        /// <returns>A query expression string.</returns>
        public virtual string GetQueryExpression() => new FieldQuery("\"" + ContentSearchHandler.GetItemTypeSection(Type) + "\"", Field.ItemType).GetQueryExpression();
    }
}