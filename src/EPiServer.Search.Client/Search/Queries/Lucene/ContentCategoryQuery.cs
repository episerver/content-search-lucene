using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EPiServer.Core;
using EPiServer.Framework;

namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// Representing a query to the Lucene Indexing Service for content with specified categories.
    /// </summary>
    public class ContentCategoryQuery : CategoryQuery
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentCategoryQuery"/> class.
        /// </summary>
        /// <param name="categories">The categories to search for.</param>
        /// <param name="luceneOperator">The lucene operator describing how to search.</param>
        public ContentCategoryQuery(CategoryList categories, LuceneOperator luceneOperator)
            : base(luceneOperator)
        {
            Validator.ThrowIfNull("categories", categories);

            foreach (var categoryId in categories)
            {
                Items.Add(categoryId.ToString());
            }
        }
    }
}
