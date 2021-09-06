using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel.Syndication;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace EPiServer.Search.IndexingService.FieldSerializers
{
    internal class CategoriesFieldStoreSerializer : TaggedFieldStoreSerializer
    {
        /// <summary>
        /// Constructs a <see cref="IndexCategories"/> from the passed <see cref="FeedItemModel"/>
        /// </summary>
        /// <param name="feedItem"></param>
        internal CategoriesFieldStoreSerializer(FeedItemModel feedItem)
            : base(feedItem)
        {
        }

        /// <summary>
        /// Constructs a <see cref="IndexCategories"/> from the passed indexFieldStore string.
        /// </summary>
        /// <param name="indexFieldStore"></param>
        internal CategoriesFieldStoreSerializer(string indexFieldStoreValue)
            : base(indexFieldStoreValue)
        {
        }

        internal override string ToFieldStoreValue()
        {
            if (FeedItem != null)
            {
                StringBuilder categories = new StringBuilder();

                foreach (string category in FeedItem.Categories)
                {
                    // Add prefix and suffix to ensure that categories with white spaces always stick together 
                    // in searches and getting them back in its original shape and form
                    categories.Append(IndexingServiceSettings.TagsPrefix);
                    categories.Append(category.Trim());
                    categories.Append(IndexingServiceSettings.TagsSuffix);
                    categories.Append(" ");
                }

                return categories.ToString().Trim();
            }
            else
            {
                return base.ToFieldStoreValue();
            }
        }

        /// <summary>
        /// Adds syndication categories to the passed syndication item either from field store string or from syndication item used to construct this CategoriesFieldStoreSerializer
        /// </summary>
        /// <param name="syndicationItem">The <see cref="SyndicationItem"/> for which to add syndication categories</param>
        internal override void AddFieldStoreValueToSyndicationItem(FeedItemModel feedItem)
        {
            if (!String.IsNullOrEmpty(FieldStoreValue))
            {
                MatchCollection matches = base.SplitFieldStoreValue();
                foreach (Match match in matches)
                {
                    feedItem.Categories.Add(base.GetOriginalValue(match.Value));
                }
            }
            else
            {
                base.AddFieldStoreValueToSyndicationItem(feedItem);
            }
        }
    }
}
