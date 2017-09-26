using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Core;
using Xunit;
using EPiServer.Search.Internal;

namespace EPiServer.Search.Queries.Lucene
{
        public class ContentQueryTests
    {
        [Fact]
        public void GetQueryExpression_WhenNoTypeSpecified_ShouldReturnQueryForIContent()
        {
            ContentQuery query = new ContentQuery();

            string result = query.GetQueryExpression();
            string expected = new FieldQuery("\"" + ContentSearchHandler.GetItemTypeSection<IContent>() + "\"", Field.ItemType).GetQueryExpression();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetQueryExpression_WhenTypeIsSpecified_ShouldReturnQueryForSpecifiedType()
        {
            ContentQuery<PageData> query = new ContentQuery<PageData>();

            string result = query.GetQueryExpression();
            string expected = new FieldQuery("\"" + ContentSearchHandler.GetItemTypeSection<PageData>() + "\"", Field.ItemType).GetQueryExpression();

            Assert.Equal(expected, result);
        }
    }
}
