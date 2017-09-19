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
        /// Constructs a <see cref="IndexCategories"/> from the passed <see cref="SyndcationItem"/>
        /// </summary>
        /// <param name="syndicationItem"></param>
        internal CategoriesFieldStoreSerializer(SyndicationItem syndicationItem) 
            : base(syndicationItem)
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
            if (SyndicationItem != null)
            {
                StringBuilder categories = new StringBuilder();

                foreach (SyndicationCategory category in SyndicationItem.Categories)
                {
                    // Add prefix and suffix to ensure that categories with white spaces always stick together 
                    // in searches and getting them back in its original shape and form
                    categories.Append(IndexingServiceSettings.TagsPrefix);
                    categories.Append(category.Name.Trim());
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
        internal override void AddFieldStoreValueToSyndicationItem(SyndicationItem syndicationItem)
        {
            if (!String.IsNullOrEmpty(FieldStoreValue))
            {
                MatchCollection matches = base.SplitFieldStoreValue();
                foreach (Match match in matches)
                {
                    syndicationItem.Categories.Add(new SyndicationCategory(base.GetOriginalValue(match.Value)));
                }
            }
            else
            {
                base.AddFieldStoreValueToSyndicationItem(syndicationItem);
            }
        }  
    }
}
