using EPiServer.Search.IndexingService.Helpers;
using Lucene.Net.Documents;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.GetReferenceData))]
    public class GetReferenceDataTest : LuceneHelperTestBase
    {
        [Fact]
        public void GetReferenceData_WhenNamedIndexReferenceDirectoryIsNull_ShouldReturnEmpty()
        {
            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.ReferenceDirectory).Returns(()=>null);

            var classInstant = SetupMock();

            var result = classInstant.GetReferenceData(It.IsAny<string>(), namedIndexMock.Object);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetReferenceData_WhenNamedIndexReferenceDirectoryIsNotNull_ShouldReturnNotEmpty()
        {
            var dir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.ReferenceDirectory).Returns(() => dir);

            
            int totalHits = 0;
            var docs = new Collection<ScoreDocument>();
            docs.Add(new ScoreDocument(new Document
            {
                new TextField(IndexingServiceSettings.TitleFieldName,"Title",Field.Store.YES),
                new TextField(IndexingServiceSettings.DisplayTextFieldName,"Body",Field.Store.YES),
                new TextField(IndexingServiceSettings.MetadataFieldName,"Meta",Field.Store.YES)
            }, 1));

            _documentHelperMock.Setup(x => x.SingleIndexSearch(It.IsAny<string>(), It.IsAny<NamedIndex>(), It.IsAny<int>(), out totalHits)).Returns(docs);

            var classInstant = SetupMock();

            var result = classInstant.GetReferenceData("1", namedIndexMock.Object);

            Assert.NotNull(result);
        }
    }
}
