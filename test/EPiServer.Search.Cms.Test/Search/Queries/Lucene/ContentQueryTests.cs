using EPiServer.Core;
using Xunit;

namespace EPiServer.Search.Queries.Lucene
{
    public class ContentQueryTests
    {
        [Fact]
        public void GetQueryExpression_WhenNoTypeSpecified_ShouldReturnQueryForIContent()
        {
            var query = new ContentQuery();

            var result = query.GetQueryExpression();
            var expected = new FieldQuery("\"" + ContentSearchHandler.GetItemTypeSection<IContent>() + "\"", Field.ItemType).GetQueryExpression();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetQueryExpression_WhenTypeIsSpecified_ShouldReturnQueryForSpecifiedType()
        {
            var query = new ContentQuery<PageData>();

            var result = query.GetQueryExpression();
            var expected = new FieldQuery("\"" + ContentSearchHandler.GetItemTypeSection<PageData>() + "\"", Field.ItemType).GetQueryExpression();

            Assert.Equal(expected, result);
        }
    }
}
