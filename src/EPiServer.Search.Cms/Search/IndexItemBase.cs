using System;
using System.Collections.ObjectModel;
using System.ServiceModel.Syndication;
using EPiServer.Search.Internal;

namespace EPiServer.Search
{
    public abstract class IndexItemBase
    {
        private SyndicationItem _syndicationItem;
        private Collection<string> _categories = new Collection<string>();
        private Collection<string> _authors = new Collection<string>();
        private Collection<string> _accessControlList = new Collection<string>();
        private Collection<string> _virtualPathNodes = new Collection<string>();
        private string _metadata;
        private IndexHtmlFilter _indexHtmlFilter;

        protected IndexItemBase(string id) 
        {
            _indexHtmlFilter = new IndexHtmlFilter();
            _syndicationItem = new SyndicationItem();
            Id = id;
            BoostFactor = 1;
            Created = DateTime.Now;
            Modified = DateTime.Now;
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
            get
            {
                return SyndicationItem.Id;
            }
            set
            {
                SyndicationItem.Id = value;
            }
        }

        /// <summary>
        /// Gets and sets the creation date for this <see cref="IndexItemBase"/>
        /// </summary>
        public DateTime Created
        {
            get
            {
                return SyndicationItem.PublishDate.DateTime;
            }
            set
            {
                if (value != DateTime.MinValue)
                {
                    SyndicationItem.PublishDate = new DateTimeOffset(value);
                }
            }
        }

        /// <summary>
        /// Gets and sets the title of this <see cref="IndexItemBase"/>
        /// </summary>
        /// <remarks></remarks>
        public string Title
        {
            get
            {
                if (SyndicationItem.Title != null)
                    return SyndicationItem.Title.Text;
                else
                    return null;
            }
            set
            {
                if (value != null)
                    SyndicationItem.Title = new TextSyndicationContent(SearchSettings.Options.HtmlStripTitle ? _indexHtmlFilter.StripHtml(value) : value);
                else
                    SyndicationItem.Title = null;
            }
        }

        /// <summary>
        /// Gets and sets the display text for this <see cref="IndexItemBase"/>
        /// </summary>
        /// <remarks></remarks>
        public string DisplayText
        {
            get
            {
                if (SyndicationItem.Content != null)
                    return ((TextSyndicationContent)SyndicationItem.Content).Text;
                else
                    return null;
            }
            set
            {
                if (value != null)
                    SyndicationItem.Content = new TextSyndicationContent(SearchSettings.Options.HtmlStripDisplayText ? _indexHtmlFilter.StripHtml(value) : value);
                else
                    SyndicationItem.Content = null;
            }
        }

        /// <summary>
        /// Gets and sets the last modified date for this <see cref="IndexItemBase"/>
        /// </summary>
        public DateTime Modified
        {
            get
            {
                return SyndicationItem.LastUpdatedTime.DateTime;
            }
            set
            {
                if (value != DateTime.MinValue)
                {
                    SyndicationItem.LastUpdatedTime = new DateTimeOffset(value);
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
            get
            {
                return _metadata;
            }
            set
            {
                if (value != null)
                    _metadata = SearchSettings.Options.HtmlStripMetadata ? _indexHtmlFilter.StripHtml(value) : value;
                else
                    _metadata = null;
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
            get
            {
                return SyndicationItem.BaseUri;
            }
            set
            {
                SyndicationItem.BaseUri = value;
            }
        }

        /// <summary>
        /// Gets and adds the categories for this <see cref="IndexItemBase"/>
        /// </summary>
        public Collection<string> Categories
        {
            get
            {
                return _categories;
            }
        }

        /// <summary>
        /// Gets and adds the authors for this <see cref="IndexItemBase"/> 
        /// </summary>
        public Collection<string> Authors
        {
            get
            {
                return _authors;
            }
        }

        /// <summary>
        /// Gets and adds a list of groups and users that has read access to this index item
        /// </summary>
        public Collection<string> AccessControlList
        {
            get
            {
                return _accessControlList;
            }
        }

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
        public Collection<string> VirtualPathNodes
        {
            get
            {
                return _virtualPathNodes;
            }
        }

        protected SyndicationItem SyndicationItem
        {
            get
            {
                return _syndicationItem;
            }
        }

        /// <summary>
        /// Returns a string wih a xml representation of a syndication feed item with custom attribute and element extentions set
        /// </summary>
        [Obsolete("Method is only supported on IndexRequestItem going forward", true)]
        protected virtual string ToSyndicationItemXml() => throw new NotSupportedException();
       
    }
}
