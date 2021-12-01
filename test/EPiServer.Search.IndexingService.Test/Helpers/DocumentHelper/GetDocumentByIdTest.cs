using System;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.DocumentHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper), nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper.GetDocumentById))]
    public class GetDocumentByIdTest : DocumentHelperTestBase
    {
        [Fact]
        public void GetDocumentById_WhenDocumentNotFound_ShouldReturnNull()
        {
            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);

            var classInstant = SetupMock();
            var result = classInstant.GetDocumentById(Guid.NewGuid().ToString(), namedIndexMock.Object);
            Assert.Null(result);
        }

        [Fact]
        public void GetDocumentById_WhenDocumentFound_ShouldReturnNotNull()
        {
            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);


            var itemId = Guid.NewGuid().ToString();
            AddDocumentForTest(namedIndexMock.Object, itemId);

            var classInstant = SetupMock();
            var result = classInstant.GetDocumentById(itemId, namedIndexMock.Object);
            Assert.NotNull(result);

            DeleteDocumentForTest(namedIndexMock.Object, itemId);
        }
    }
}
