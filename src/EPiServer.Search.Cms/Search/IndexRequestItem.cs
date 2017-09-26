using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace EPiServer.Search
{
    public class IndexRequestItem : IndexItemBase
    { 
        public IndexRequestItem(string id, IndexAction indexAction): 
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
        public virtual string ToSyndicationItemXml(SearchOptions options)
        {
            // Add index action (add, update or remove) as an attribute extension
            var key = new XmlQualifiedName(options.SyndicationItemAttributeNameIndexAction, options.XmlQualifiedNamespace);
            SyndicationItem.AttributeExtensions[key] = SearchSettings.GetIndexActionName(IndexAction);

            // Add AutoUpdateVirtualPath if set
            key = new XmlQualifiedName(options.SyndicationItemAttributeNameAutoUpdateVirtualPath, options.XmlQualifiedNamespace);
            if (AutoUpdateVirtualPath.HasValue)
            {
                SyndicationItem.AttributeExtensions[key] = AutoUpdateVirtualPath.Value.ToString(CultureInfo.InvariantCulture);
            }
            else if (SyndicationItem.AttributeExtensions.ContainsKey(key))
            {
                SyndicationItem.AttributeExtensions.Remove(key);
            }

            SyndicationItem.AttributeExtensions.Add(new XmlQualifiedName(options.SyndicationItemAttributeNameBoostFactor, options.XmlQualifiedNamespace), BoostFactor.ToString(CultureInfo.InvariantCulture.NumberFormat));
            SyndicationItem.AttributeExtensions.Add(new XmlQualifiedName(options.SyndicationItemAttributeNameType, options.XmlQualifiedNamespace), ItemType);
            SyndicationItem.AttributeExtensions.Add(new XmlQualifiedName(options.SyndicationItemAttributeNameCulture, options.XmlQualifiedNamespace), Culture);
            SyndicationItem.AttributeExtensions.Add(new XmlQualifiedName(options.SyndicationItemAttributeNameNamedIndex, options.XmlQualifiedNamespace), NamedIndex);
            SyndicationItem.AttributeExtensions.Add(new XmlQualifiedName(options.SyndicationItemAttributeNameReferenceId, options.XmlQualifiedNamespace), ReferenceId);
            SyndicationItem.AttributeExtensions.Add(new XmlQualifiedName(options.SyndicationItemAttributeNameItemStatus, options.XmlQualifiedNamespace), ((int)ItemStatus).ToString(CultureInfo.InvariantCulture));

            if (PublicationEnd.HasValue)
            {
                SyndicationItem.AttributeExtensions.Add(new XmlQualifiedName(options.SyndicationItemAttributeNamePublicationEnd, options.XmlQualifiedNamespace), PublicationEnd.Value.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
            }

            if (PublicationStart.HasValue)
            {
                SyndicationItem.AttributeExtensions.Add(new XmlQualifiedName(options.SyndicationItemAttributeNamePublicationStart, options.XmlQualifiedNamespace), PublicationStart.Value.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
            }

            if (DataUri != null)
            {
                SyndicationItem.AttributeExtensions.Add(new XmlQualifiedName(options.SyndicationItemAttributeNameDataUri, options.XmlQualifiedNamespace), DataUri.ToString());
            }

            // Add metadata extension element
            SyndicationItem.ElementExtensions.Add(
                new SyndicationElementExtension(options.SyndicationItemElementNameMetadata,
                options.XmlQualifiedNamespace, Metadata));

            foreach (string categoryName in Categories.Where(c => !string.IsNullOrEmpty(c)))
            {
                SyndicationItem.Categories.Add(new SyndicationCategory(categoryName));
            }

            foreach (string author in Authors.Where(a => !string.IsNullOrEmpty(a)))
            {
                SyndicationItem.Authors.Add(new SyndicationPerson("", author, ""));
            }

            XNamespace ns = options.XmlQualifiedNamespace;
            // Add Read Access Control List element
            XElement element = new XElement(ns + "ACL");
            foreach (string access in AccessControlList.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                element.Add(new XElement(ns + "Item", access));
            }
            SyndicationItem.ElementExtensions.Add(element.CreateReader());

            // Add virtual path element
            element = new XElement(ns + "VirtualPath");
            foreach (string item in VirtualPathNodes)
            {
                element.Add(new XElement(ns + "Item", item.Replace(" ", "")));
            }
            SyndicationItem.ElementExtensions.Add(element.CreateReader());

            // Get XML string for SyndicationItem
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.CheckCharacters = false;
            using (XmlWriter writer = XmlWriter.Create(sb, xws))
            {
                SyndicationItem.GetAtom10Formatter().WriteTo(writer);
            }

            return sb.ToString();
        }
    }
}
