using System;
using System.Collections.ObjectModel;
using Lucene.Net.Documents;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.GetScoreDocuments))]
    public class GetScoreDocumentsTest : LuceneHelperTestBase
    {
        [Fact]
        public void GetScoreDocuments_WhenNamedIndexIsNull_ShouldThrowExeption()
        {
            _commonFuncMock.Setup(x => x.PrepareExpression(It.IsAny<string>(), It.IsAny<bool>())).Returns("something");

            var totalHits = 0;
            var classInstant = SetupMock();

            // Act
            var caughtException = Assert.Throws<ArgumentNullException>(() => classInstant.GetScoreDocuments("a:b", true, null, 1, 1, 1, out totalHits));

            // Assert
            Assert.Equal("namedIndexes", caughtException.ParamName);
        }

        [Fact]
        public void GetScoreDocuments_WhenNamedIndexCountZero_ShouldThrowExeption()
        {
            _commonFuncMock.Setup(x => x.PrepareExpression(It.IsAny<string>(), It.IsAny<bool>())).Returns("something");

            var totalHits = 0;
            var classInstant = SetupMock();

            // Act
            var caughtException = Assert.Throws<ArgumentException>(() => classInstant.GetScoreDocuments("a:b", true, new System.Collections.ObjectModel.Collection<NamedIndex>(), 1, 1, 1, out totalHits));

            // Assert
            Assert.Equal("namedIndexes", caughtException.ParamName);
        }

        [Fact]
        public void GetScoreDocuments_WhenNamedIndexCountOne_ShouldReturnOneDocument()
        {
            _commonFuncMock.Setup(x => x.PrepareExpression(It.IsAny<string>(), It.IsAny<bool>())).Returns("something");

            var totalHits = 1;

            var dir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir);

            var namedIndexs = new System.Collections.ObjectModel.Collection<NamedIndex>() { namedIndexMock.Object };

            var docs = new Collection<ScoreDocument>
            {
                new ScoreDocument(new Document
            {
                new TextField(IndexingServiceSettings.TitleFieldName,"Title",Field.Store.YES),
                new TextField(IndexingServiceSettings.DisplayTextFieldName,"Body",Field.Store.YES),
                new TextField(IndexingServiceSettings.MetadataFieldName,"Meta",Field.Store.YES)
            }, 1)
            };

            _documentHelperMock.Setup(x => x.SingleIndexSearch(It.IsAny<string>(), It.IsAny<NamedIndex>(), It.IsAny<int>(), out totalHits)).Returns(docs);

            var classInstant = SetupMock();
            var result = classInstant.GetScoreDocuments("a:b", true, namedIndexs, 0, 1, 1, out totalHits);
            Assert.Single(result);
        }
        [Fact]
        public void GetScoreDocuments_WhenNamedIndexCountMulti_ShouldReturnMultiDocument()
        {
            _commonFuncMock.Setup(x => x.PrepareExpression(It.IsAny<string>(), It.IsAny<bool>())).Returns("something");

            var totalHits = 2;

            var dir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));
            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir);

            var dir2 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));
            var namedIndexMock2 = new Mock<NamedIndex>("testindex2");
            namedIndexMock2.SetupGet(x => x.Directory).Returns(() => dir2);

            var namedIndexs = new System.Collections.ObjectModel.Collection<NamedIndex>() { namedIndexMock.Object, namedIndexMock2.Object };

            var docs = new Collection<ScoreDocument>
            {
                new ScoreDocument(new Document
            {
                new TextField(IndexingServiceSettings.TitleFieldName,"Title1",Field.Store.YES),
                new TextField(IndexingServiceSettings.DisplayTextFieldName,"Body1",Field.Store.YES),
                new TextField(IndexingServiceSettings.MetadataFieldName,"Meta1",Field.Store.YES)
            }, 1),
                new ScoreDocument(new Document
            {
                new TextField(IndexingServiceSettings.TitleFieldName,"Title2",Field.Store.YES),
                new TextField(IndexingServiceSettings.DisplayTextFieldName,"Body2",Field.Store.YES),
                new TextField(IndexingServiceSettings.MetadataFieldName,"Meta2",Field.Store.YES)
            }, 1)
            };

            _documentHelperMock.Setup(x => x.MultiIndexSearch(It.IsAny<string>(), It.IsAny<Collection<NamedIndex>>(), It.IsAny<int>(), out totalHits)).Returns(docs);

            var classInstant = SetupMock();
            var result = classInstant.GetScoreDocuments("a:b", true, namedIndexs, 0, 2, 1, out totalHits);
            Assert.Equal(2, result.Count);
        }
    }
}
