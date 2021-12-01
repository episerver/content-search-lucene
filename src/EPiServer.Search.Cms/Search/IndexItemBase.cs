using System;
using System.Collections.ObjectModel;
using EPiServer.Models;
using EPiServer.Search.Internal;

namespace EPiServer.Search
{
    public abstract class IndexItemBase
    {
        private readonly FeedItemModel _syndicationItem;
        private readonly Collection<string> _categories = new Collection<string>();
        private readonly Collection<string> _authors = new Collection<string>();
        private readonly Collection<string> _accessControlList = new Collection<string>();
        private readonly Collection<string> _virtualPathNodes = new Collection<string>();
        private string _metadata;
        private readonly IndexHtmlFilter _indexHtmlFilter;

        protected IndexItemBase(string id)
        {
            _indexHtmlFilter = new IndexHtmlFilter();
            _syndicationItem = new FeedItemModel();
            Id = id;
            BoostFactor = 1;
            Created = DateTimeOffset.Now;
            Modified = DateTimeOffset.Now;
            Title = string.Empty;
            DisplayText = string.Empty;
            Metadata = string.Empty;
            ItemType = string.Empty;
            Culture = string.Empty;
            NamedIndex = string.Empty;
            ItemStatus = ItemStatus.Approved;

        }

        /// <summary>
        /// Gets and sets the unique Id for this <see cref="IndexItemBase"/>. The ID needs to be unique within a named index.
        /// </summary>
        public string Id
        {
            get => SyndicationItem.Id;
            set => SyndicationItem.Id = value;
        }

        /// <summary>
        /// Gets and sets the creation date for this <see cref="IndexItemBase"/>
        /// </summary>
        public DateTimeOffset Created
        {
            get => SyndicationItem.Created.DateTime;
            set
            {
                if (value != DateTimeOffset.MinValue)
                {
                    SyndicationItem.Created = value;
                }
            }
        }

        /// <summary>
        /// Gets and sets the title of this <see cref="IndexItemBase"/>
        /// </summary>
        /// <remarks></remarks>
        public string Title
        {
            get => SyndicationItem.Title;
            set => SyndicationItem.Title = value;
        }

        /// <summary>
        /// Gets and sets the display text for this <see cref="IndexItemBase"/>
        /// </summary>
        /// <remarks></remarks>
        public string DisplayText
        {
            get => SyndicationItem.DisplayText;
            set => SyndicationItem.DisplayText = value;
        }

        /// <summary>
        /// Gets and sets the last modified date for this <see cref="IndexItemBase"/>
        /// </summary>
        public DateTimeOffset Modified
        {
            get => SyndicationItem.Modified;
            set
            {
                if (value != DateTimeOffset.MinValue)
                {
                    SyndicationItem.Modified = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets expiration date for this <see cref="IndexItemBase"/> for when it should not be returned in index searches
        /// </summary>
        public DateTime? PublicationEnd
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets start date for this <see cref="IndexItemBase"/> for when it should be returned in index searches
        /// </summary>
        public DateTime? PublicationStart
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the <see cref="ItemStatus"/> for this <see cref="IndexItemBase"/>
        /// </summary>
        public ItemStatus ItemStatus
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets additional metadata for this <see cref="IndexItemBase"/>
        /// </summary>
        public string Metadata
        {
            get => _metadata;
            set
            {
                if (value != null)
                {
                    _metadata = SearchSettings.Options.HtmlStripMetadata ? _indexHtmlFilter.StripHtml(value) : value;
                }
                else
                {
                    _metadata = null;
                }
            }
        }

        /// <summary>
        /// Gets and sets the item type for this <see cref="IndexItemBase"/>
        /// </summary>
        public string ItemType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the culture for this <see cref="IndexItemBase"/>
        /// </summary>
        public string Culture
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the boost factor for which an <see cref="IndexItemBase"/> should be weighted in indexing service
        /// Note that this feature needs to be supported by the current indexing service
        /// </summary>
        public float BoostFactor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the name of the index that this <see cref="IndexItemBase"/> should update
        /// </summary>
        public string NamedIndex
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the Uri for this <see cref="IndexItemBase"/> to be used for linking in search results.
        /// </summary>
        public Uri Uri
        {
            get => SyndicationItem.Uri;
            set => SyndicationItem.Uri = value;
        }

        /// <summary>
        /// Gets and adds the categories for this <see cref="IndexItemBase"/>
        /// </summary>
        public Collection<string> Categories => _categories;

        /// <summary>
        /// Gets and adds the authors for this <see cref="IndexItemBase"/> 
        /// </summary>
        public Collection<string> Authors => _authors;

        /// <summary>
        /// Gets and adds a list of groups and users that has read access to this index item
        /// </summary>
        public Collection<string> AccessControlList => _accessControlList;

        /// <summary>
        /// gets and sets the reference id, which is the id of the parent item
        /// </summary>
        public string ReferenceId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets a Uri to where the indexing service should fetch data and add to this <see cref="IndexItemBase"/>
        /// </summary>
        public Uri DataUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of nodes used to contruct the virtual path to this item. 
        /// The virtual path is used to connect the item to a node in a tree structure and hence allowing searches under a specific node
        /// This default implementation will remove all white spaces in a node value
        /// </summary>
        public Collection<string> VirtualPathNodes => _virtualPathNodes;

        protected FeedItemModel SyndicationItem => _syndicationItem;

        /// <summary>
        /// Returns a string wih a xml representation of a syndication feed item with custom attribute and element extentions set
        /// </summary>
        [Obsolete("Method is only supported on IndexRequestItem going forward", true)]
        protected virtual string ToSyndicationItemXml() => throw new NotSupportedException();

    }
}
