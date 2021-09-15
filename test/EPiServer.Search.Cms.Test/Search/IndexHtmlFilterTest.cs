using System;
using EPiServer.Search.Internal;
using Xunit;

namespace EPiServer.Search
{
    public class IndexHtmlFilterTest
    {
        [Fact]
        public void StripHtml_WithNestedElements_ShouldExtractText()
        {
            var result = new IndexHtmlFilter().StripHtml("<p>WORD1<br />WORD2 WORD3</p>");

            Assert.Equal("WORD1" + Environment.NewLine + "WORD2 WORD3", result);
        }

        [Fact]
        public void StripHtml_WithoutHtml_ShouldExtractText()
        {
            var result = new IndexHtmlFilter().StripHtml("WORD1 WORD2 WORD3");

            Assert.Equal("WORD1 WORD2 WORD3", result);
        }

        [Fact]
        public void StripHtml_WithMultiLineHtml_ShouldExtractText()
        {
            var result = new IndexHtmlFilter().StripHtml("<p>WORD1 WORD2 WORD3</p>" + Environment.NewLine + "<p>WORD4 WORD5 WORD6</p>");

            Assert.Equal("WORD1 WORD2 WORD3" + Environment.NewLine + "WORD4 WORD5 WORD6", result);
        }

        [Fact]
        public void StripHtml_WithMultiLineHtml_ShouldNotDuplicateNewLines()
        {
            var result = new IndexHtmlFilter().StripHtml("<p>WORD1 WORD2 WORD3</p><p></p><br><br>" + Environment.NewLine + "<p>WORD4 WORD5 WORD6</p>");

            Assert.Equal("WORD1 WORD2 WORD3" + Environment.NewLine + "WORD4 WORD5 WORD6", result);
        }

        [Fact]
        public void StripHtml_WithNonBlockHtmlTags_ShouldNotAddNewLines()
        {
            var result = new IndexHtmlFilter().StripHtml("<p>WORD1 <span>WORD2</span> WO<i>R</i>D3</p>");

            Assert.Equal("WORD1 WORD2 WORD3", result);
        }
    }
}
