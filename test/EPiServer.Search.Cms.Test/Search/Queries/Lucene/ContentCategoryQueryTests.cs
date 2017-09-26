using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Core;
using Xunit;

namespace EPiServer.UnitTests.Search.Queries.Lucene
{
        public class ContentCategoryQueryTests
    {
        [Fact]
        public void Constructor_WhenCategoryListIsNull_ShouldThrowException()
        {
            Assert.Throws<ArgumentNullException>(() => new ContentCategoryQuery(null, LuceneOperator.AND));
        }

        [Fact]
        public void Constructor_WhenCategoryHasValues_ShouldAddIdsToItems()
        {
            CategoryList categories = new CategoryList(new int[] { 2, 5 });

            ContentCategoryQuery query = new ContentCategoryQuery(categories, LuceneOperator.AND);

            Assert.Equal(2, query.Items.Count);
            Assert.True(query.Items.Contains(2.ToString()));
            Assert.True(query.Items.Contains(5.ToString()));
        }
    }
}
