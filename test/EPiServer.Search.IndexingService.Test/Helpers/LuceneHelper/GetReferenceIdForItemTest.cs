using Lucene.Net.Documents;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.GetReferenceIdForItem))]
    public class GetReferenceIdForItemTest : LuceneHelperTestBase
    {
        [Fact]
        public void GetReferenceIdForItem_WhenDocIsNull_ShouldReturnNull()
        {
            _documentHelperMock.Setup(x => x.GetDocumentById(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns((Document)null);
            var classInstant = SetupMock();
            var result = classInstant.GetReferenceIdForItem("vp1", new NamedIndex("testindex"));
            Assert.Null(result);
        }

        [Fact]
        public void GetReferenceIdForItem_WhenDocIsNotNull_ShouldReturnReferenceId()
        {
            var doc = new Document
            {
                new TextField(IndexingServiceSettings.ReferenceIdFieldName, "testindex_ref", Field.Store.YES)
            };
            _documentHelperMock.Setup(x => x.GetDocumentById(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(doc);

            var classInstant = SetupMock();
            var result = classInstant.GetReferenceIdForItem("vp1", new NamedIndex("testindex"));
            Assert.Equal("testindex_ref", result);
        }
    }
}
