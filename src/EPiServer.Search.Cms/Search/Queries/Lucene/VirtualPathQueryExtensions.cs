using System;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace EPiServer.Search.Queries.Lucene
{
    public static class VirtualPathQueryExtensions
    {

        /// <summary>
        /// Adds the path nodes of the content item referenced by the provided content link to the query.
        /// </summary>
        /// <param name="query">The query to extend.</param>
        /// <param name="contentLink">The content link.</param>
        public static void AddContentNodes(this VirtualPathQuery query, ContentReference contentLink) => query.AddContentNodes(contentLink, ServiceLocator.Current.GetInstance<ContentSearchHandler>());

        public static void AddContentNodes(this VirtualPathQuery query, ContentReference contentLink, ContentSearchHandler searchHandler)
        {
            if (ContentReference.IsNullOrEmpty(contentLink))
            {
                return;
            }

            foreach (var node in searchHandler.GetVirtualPathNodes(contentLink))
            {
                query.VirtualPathNodes.Add(node);
            }
        }


        /// <summary>
        /// Adds the path nodes of the content item referenced by the provided content link to the query.
        /// </summary>
        /// <param name="query">The query to extend.</param>
        /// <param name="contentLink">The content link.</param>
        /// <param name="contentLoader">The content queryable.</param>
        /// <exclude />
        [Obsolete("Use override that does not take IContentLoader")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void AddContentNodes(this VirtualPathQuery query, ContentReference contentLink, IContentLoader contentLoader) => query.AddContentNodes(contentLink);

    }
}
