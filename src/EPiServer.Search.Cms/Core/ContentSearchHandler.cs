using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer.Framework;
using EPiServer.Search;
using EPiServer.ServiceLocation;
using EPiServer.Search.Internal;

namespace EPiServer.Core
{
    /// <summary>
    /// Manages the index of searchable content item information.
    /// </summary>
    /// <example>
    /// <para>
    /// Example of how you could search for pages.
    /// </para>
    /// <code source="../CodeSamples/EPiServer/Search/SearchQuerySamples.cs" region="PageSearch" lang="cs" />
    /// </example>
    public abstract class ContentSearchHandler : IReIndexable
    {
        /// <summary>
        /// The identifier used to store invariant culture name.
        /// </summary>
        /// <remarks><see cref="CultureInfo.Name"/> for <see cref="CultureInfo.InvariantCulture"/> is String.Empty which is not possible to query for 
        /// therfore content stored in <see cref="CultureInfo.InvariantCulture"/> is indexed with "iv" as culture name.</remarks>
        public const string InvariantCultureIndexedName = "iv";

        /// <summary>
        /// Gets or sets a value indicating whether the search service is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if search service is active; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>This value will be initialized from the configuration settings of EPiServer.Search.</remarks>
        public abstract bool ServiceActive
        {
            get;set;
        }

        /// <summary>
        /// Adds all published content to the index by calling this.UpdateIndex for each item under RootPage
        /// </summary>
        public abstract void IndexPublishedContent();

        /// <summary>
        /// Updates the search index representation of the provided content item.
        /// </summary>
        /// <param name="contentItem">The content item that should be re-indexed.</param>
        /// <remarks>
        /// Note that only the exact language version that is provided is updated. If you want to 
        /// update all language versions of a page, use alternative method overload.
        /// </remarks>
        public abstract void UpdateItem(IContent contentItem);

        /// <summary>
        /// Updates the search index for the provided content item and it's descendants 
        /// with a new virtual path location.
        /// </summary>
        /// <param name="contentLink">The reference to the content item that is the root item that should get a new virtual path in the search index.</param>
        /// <remarks>
        /// The content of the provided item will also be included as a part of the update.
        /// </remarks>
        public abstract void MoveItem(ContentReference contentLink);

        /// <summary>
        /// Removes all content items located at or under the provided virtual node from the search index.
        /// This will include all language versions as well.
        /// </summary>
        /// <param name="virtualPathNodes">The collection of virtual path nodes used to determine what items to remove.</param>
        public abstract void RemoveItemsByVirtualPath(ICollection<string> virtualPathNodes);

        /// <summary>
        /// Removes a language branch of a content item from the search index.
        /// </summary>
        /// <param name="contentItem">The content item that should be removed from the search index.</param>
        public abstract void RemoveLanguageBranch(IContent contentItem);

        /// <summary>
        /// Gets the item type representation for the provided content item type that is used in the search index.
        /// </summary>
        /// <typeparam name="T">Type of the content</typeparam>
        /// <returns>
        /// A string representing the full ItemType.
        /// </returns>
        /// <remarks>
        /// This string will be made up by the base type of the provided type together with a generic name
        /// idicating that it is a content item.
        /// </remarks>
        public virtual string GetItemType<T>()
        {
            return GetItemType(typeof(T));
        }

        /// <summary>
        /// Gets the item type representation for the provided content item type that is used in the search index.
        /// </summary>
        /// <param name="contentType">Type of the content.</param>
        /// <returns>
        /// A string representing the full ItemType.
        /// </returns>
        /// <remarks>
        /// This string will be made up by the base type of the provided type together with a generic name
        /// idicating that it is a content item.
        /// </remarks>
        public abstract string GetItemType(Type contentType);

        /// <summary>
        /// Gets the section that the provided type appends to the ItemType field of the search index.
        /// </summary>
        /// <typeparam name="T">The type of item in the index</typeparam>
        /// <returns>
        /// A string that represents the type in the ItemType field.
        /// </returns>
        public static string GetItemTypeSection<T>()
        {
            return GetItemTypeSection(typeof(T));
        }

        /// <summary>
        /// Gets the section that the provided type appends to the ItemType field of the search index.
        /// </summary>
        /// <param name="type">The type of item in the index.</param>
        /// <returns>
        /// A string that represents the type in the ItemType field.
        /// </returns>
        public static string GetItemTypeSection(Type type)
        {
            Validator.ThrowIfNull("type", type);

            return type.FullName + "," + type.Assembly.GetName().Name;
        }

        /// <summary>
        /// Converts the <paramref name="indexItem"/> to the correct <see cref="IContent"/> instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="indexItem">The index item.</param>
        /// <returns>
        ///   <c>null</c> if <paramref name="indexItem"/> is not valid; otherwise a <see cref="PageData"/> instance.
        /// </returns>
        /// <remarks>
        ///   <para>The Id of <paramref name="indexItem"/> must start with a guid that matches a page.</para>
        ///   <para>It will use the Culture of <paramref name="indexItem"/> to specify of what culture the returned <see cref="IContent"/> should be.</para>
        /// </remarks>
        public virtual T GetContent<T>(IndexItemBase indexItem)
            where T : IContent
        {
            return GetContent<T>(indexItem, false);
        }

        /// <summary>
        /// Converts the <paramref name="indexItem"/> to the correct <see cref="IContent"/> instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="indexItem">The index item.</param>
        /// <param name="filterOnCulture">if set to <c>true</c> filter on culture.</param>
        /// <returns>
        ///   <c>null</c> if <paramref name="indexItem"/> is not valid; otherwise a <see cref="PageData"/> instance.
        /// </returns>
        /// <remarks>
        ///   <para>The Id of <paramref name="indexItem"/> must start with a guid that matches a page.</para>
        ///   <para>if <paramref name="filterOnCulture"/> is <c>false</c> it will use the Culture of <paramref name="indexItem"/> 
        ///     to specify of what culture the returned <see cref="IContent"/> should be. If <paramref name="filterOnCulture"/>
        ///     is <c>false</c> it will only get content in the current culture.</para>
        /// </remarks>
        public abstract T GetContent<T>(IndexItemBase indexItem, bool filterOnCulture) where T : IContent;

        /// <summary>
        /// Gets the search result for the specified query.
        /// </summary>
        /// <typeparam name="T">The type of content that should be returned.</typeparam>
        /// <param name="searchQuery">The search query.</param>
        /// <param name="page">The page index of the result. Used to handle paging. Most be larger than 0.</param>
        /// <param name="pageSize">Number of items per page. Used to handle paging.</param>
        /// <returns>
        /// The search result matching the search query.
        /// </returns>
        public virtual SearchResults GetSearchResults<T>(string searchQuery, int page, int pageSize)
             where T : IContent
        {
            return GetSearchResults<T>(searchQuery, ContentReference.EmptyReference, page, pageSize, true);
        }

        /// <summary>
        /// Gets the search result for the specified query.
        /// </summary>
        /// <typeparam name="T">The type of content that should be returned.</typeparam>
        /// <param name="searchQuery">The search query.</param>
        /// <param name="root">The root for the search.</param>
        /// <param name="page">The page index of the result. Used to handle paging. Most be larger than 0.</param>
        /// <param name="pageSize">Number of items per page. Used to handle paging.</param>
        /// <param name="filterOnAccess">if set to <c>true</c>, items that the user doesn't have read access to will be removed.</param>
        /// <returns>
        /// The search result matching the search query.
        /// </returns>
        public abstract SearchResults GetSearchResults<T>(string searchQuery, ContentReference root, int page, int pageSize, bool filterOnAccess) where T : IContent;

        /// <summary>
        /// Gets a collection of virtual path nodes for a content item to use in the search index.
        /// </summary>
        /// <param name="contentLink">The content link.</param>
        /// <returns>A collection of virtual path nodes.</returns>
        public abstract ICollection<string> GetVirtualPathNodes(ContentReference contentLink);

        /// <summary>
        /// Gets the culture identifier that is used when indexing a content item.
        /// </summary>
        /// <remarks>
        /// Normally <see cref="CultureInfo.Name"/> is used. However for <see cref="CultureInfo.InvariantCulture"/> is
        /// <see cref="InvariantCultureIndexedName"/> used.
        /// </remarks>
        /// <param name="culture">The culture.</param>
        /// <returns></returns>
        public static string GetCultureIdentifier(CultureInfo culture)
        {
            return culture != null
                       ? (CultureInfo.InvariantCulture.Equals(culture) ? InvariantCultureIndexedName : culture.Name)
                       : String.Empty;
        }

        /// <summary>
        /// Implementation of <see cref="IReIndexable"/>, forwards to <see cref="M:IndexPublishedContent"/>
        /// </summary>
        public void ReIndex()
        {
            IndexPublishedContent();
        }

        /// <summary>
        /// Gets the index of the named.
        /// </summary>
        public abstract string NamedIndex
        {
            get;
        }

        /// <summary>
        /// Gets the named indexing service.
        /// </summary>
        public abstract string NamedIndexingService
        {
            get;
        }
    }
}
