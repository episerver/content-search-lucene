using Lucene.Net.Documents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.UpdateReference))]
    public class UpdateReferenceTest : LuceneHelperTestBase
    {
        [Fact]
        public void UpdateReference_WhenMainDocIsNull_ShouldReturnFalse()
        {
            var logMock = new Mock<ILogger>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            _documentHelperMock.Setup(x => x.GetDocumentById(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns((Document)null);

            var classInstant = SetupMock();
            var result = classInstant.UpdateReference("1", "2", new NamedIndex("testindex1"));
            Assert.False(result);
        }

        [Fact]
        public void UpdateReference_WhenMainDocIsNotNull_ShouldReturnTrue()
        {
            var logMock = new Mock<ILogger>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            var doc = new Document
            {
                new TextField(IndexingServiceSettings.VirtualPathFieldName, "vp", Field.Store.YES)
            };
            _documentHelperMock.Setup(x => x.GetDocumentById(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(doc);

            var namedIndexMock = new Mock<NamedIndex>("testindex1");

            var classInstant = SetupMock();
            var result = classInstant.UpdateReference("1", "2", namedIndexMock.Object);
            Assert.True(result);
        }
    }
}
