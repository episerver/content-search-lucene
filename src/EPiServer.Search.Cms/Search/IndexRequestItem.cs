using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace EPiServer.Search
{
    public class IndexRequestItem : IndexItemBase
    {
        public IndexRequestItem(string id, IndexAction indexAction) :
            base(id)
        {
            IndexAction = indexAction;
        }

        /// <summary>
        /// Gets and sets the actions (Add, Update or Remove) that this <see cref="IndexRequestItem"/> is suppose to perform in indexing service
        /// </summary>
        public IndexAction IndexAction { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to auto update other items below the provided virtual path.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if other items should be auto updated; otherwise, <c>false</c>.
        /// </value>
        public bool? AutoUpdateVirtualPath { get; set; }

        /// <summary>
        /// Serialize this <see cref="IndexRequestItem"/> to an XML fragment with an Atom Feed entry element.
        /// </summary>
        /// <returns>An XML fragment string with an Atom feed entry element.</returns>
        public virtual string ToFeedItemJson(SearchOptions options)
        {
            // Add index action (add, update or remove) as an attribute extension
            var key = options.SyndicationItemAttributeNameIndexAction;
            SyndicationItem.AttributeExtensions[key] = SearchSettings.GetIndexActionName(IndexAction);

            // Add AutoUpdateVirtualPath if set
            key = options.SyndicationItemAttributeNameAutoUpdateVirtualPath;
            if (AutoUpdateVirtualPath.HasValue)
            {
                SyndicationItem.AttributeExtensions[key] = AutoUpdateVirtualPath.Value.ToString(CultureInfo.InvariantCulture);
            }
            else if (SyndicationItem.AttributeExtensions.ContainsKey(key))
            {
                SyndicationItem.AttributeExtensions.Remove(key);
            }

            SyndicationItem.AttributeExtensions.Add(options.SyndicationItemAttributeNameBoostFactor, BoostFactor.ToString(CultureInfo.InvariantCulture.NumberFormat));
            SyndicationItem.AttributeExtensions.Add(options.SyndicationItemAttributeNameType, ItemType);
            SyndicationItem.AttributeExtensions.Add(options.SyndicationItemAttributeNameCulture, Culture);
            SyndicationItem.AttributeExtensions.Add(options.SyndicationItemAttributeNameNamedIndex, NamedIndex);
            SyndicationItem.AttributeExtensions.Add(options.SyndicationItemAttributeNameReferenceId, ReferenceId);
            SyndicationItem.AttributeExtensions.Add(options.SyndicationItemAttributeNameItemStatus, ((int)ItemStatus).ToString(CultureInfo.InvariantCulture));

            if (PublicationEnd.HasValue)
            {
                SyndicationItem.AttributeExtensions.Add(options.SyndicationItemAttributeNamePublicationEnd, PublicationEnd.Value.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
            }

            if (PublicationStart.HasValue)
            {
                SyndicationItem.AttributeExtensions.Add(options.SyndicationItemAttributeNamePublicationStart, PublicationStart.Value.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
            }

            if (DataUri != null)
            {
                SyndicationItem.AttributeExtensions.Add(options.SyndicationItemAttributeNameDataUri, DataUri.ToString());
            }

            // Add metadata extension element
            SyndicationItem.ElementExtensions.Add(options.SyndicationItemElementNameMetadata, Metadata);

            foreach (var categoryName in Categories.Where(c => !string.IsNullOrEmpty(c)))
            {
                SyndicationItem.Categories.Add(categoryName);
            }

            foreach (var author in Authors.Where(a => !string.IsNullOrEmpty(a)))
            {
                SyndicationItem.Authors.Add(author);
            }

            var acessControlList = new Collection<string>();
            foreach (var access in AccessControlList.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                acessControlList.Add(access);
            }

            SyndicationItem.ElementExtensions.Add(SearchSettings.Options.SyndicationItemElementNameAcl, acessControlList);

            // Add virtual path element
            var virtualpaths = new Collection<string>();
            foreach (var item in VirtualPathNodes)
            {
                virtualpaths.Add(item.Replace(" ", ""));
            }
            SyndicationItem.ElementExtensions.Add(SearchSettings.Options.SyndicationItemElementNameVirtualPath, virtualpaths);

            // Get Json string for SyndicationItem
            return JsonSerializer.Serialize(SyndicationItem);
        }
    }
}
