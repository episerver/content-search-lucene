using System;
using Lucene.Net.Documents;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(IndexingService.Helpers.LuceneHelper), nameof(IndexingService.Helpers.LuceneHelper.WriteToIndex))]
    public class WriteToIndexTest : LuceneHelperTestBase
    {
        [Fact]
        public void WriteToIndexTest_WhenDocumentExists_ShouldReturnFalse()
        {
            var doc = new Document();
            var namedIndex = new NamedIndex("testindex1");
            _documentHelperMock.Setup(x => x.DocumentExists(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(true);

            var classInstant = SetupMock();
            var result = classInstant.WriteToIndex("1", doc, namedIndex);

            Assert.False(result);
        }

        [Fact]
        public void WriteToIndexTest_WhenDocumentNotExists_ShouldReturnTrue()
        {
            var doc = new Document();
            var dir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir);

            _documentHelperMock.Setup(x => x.DocumentExists(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(false);

            var classInstant = SetupMock();
            var result = classInstant.WriteToIndex("1", doc, namedIndexMock.Object);

            Assert.True(result);
        }
    }
}
