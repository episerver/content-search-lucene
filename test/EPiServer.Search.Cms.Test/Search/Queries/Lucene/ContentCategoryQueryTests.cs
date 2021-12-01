using System;
using EPiServer.Core;
using EPiServer.Search.Queries.Lucene;
using Xunit;

namespace EPiServer.UnitTests.Search.Queries.Lucene
{
    public class ContentCategoryQueryTests
    {
        [Fact]
        public void Constructor_WhenCategoryListIsNull_ShouldThrowException() => Assert.Throws<ArgumentNullException>(() => new ContentCategoryQuery(null, LuceneOperator.AND));

        [Fact]
        public void Constructor_WhenCategoryHasValues_ShouldAddIdsToItems()
        {
            var categories = new CategoryList(new int[] { 2, 5 });

            var query = new ContentCategoryQuery(categories, LuceneOperator.AND);

            Assert.Equal(2, query.Items.Count);
            Assert.True(query.Items.Contains(2.ToString()));
            Assert.True(query.Items.Contains(5.ToString()));
        }
    }
}
