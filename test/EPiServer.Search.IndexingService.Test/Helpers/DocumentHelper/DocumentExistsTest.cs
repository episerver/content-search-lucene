using System;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.DocumentHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper), nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper.DocumentExists))]
    public class DocumentExistsTest : DocumentHelperTestBase
    {
        [Fact]
        public void DocumentExists_WhenDocumentNotExist_ShouldReturnFalse()
        {
            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);

            var classInstant = SetupMock();
            var result = classInstant.DocumentExists(Guid.NewGuid().ToString(), namedIndexMock.Object);
            Assert.False(result);
        }
        [Fact]
        public void DocumentExists_WhenDocumentExists_ShouldReturnTrue()
        {
            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);

            var itemId = Guid.NewGuid().ToString();
            AddDocumentForTest(namedIndexMock.Object, itemId);

            var classInstant = SetupMock();
            var result = classInstant.DocumentExists(itemId, namedIndexMock.Object);
            Assert.True(result);

            DeleteDocumentForTest(namedIndexMock.Object, itemId);
        }
    }
}
