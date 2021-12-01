using System;
using System.Collections.ObjectModel;
using Lucene.Net.Documents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.RemoveByVirtualPath))]
    public class RemoveByVirtualPathTest : LuceneHelperTestBase
    {
        [Fact]
        public void RemoveByVirtualPath_WhenVirtualPathIsEmpty_ShouldReturnFalse()
        {
            var classInstant = SetupMock();
            var result = classInstant.RemoveByVirtualPath("");

            Assert.False(result);
        }

        [Fact]
        public void RemoveByVirtualPath_WhenVirtualPathIsNotEmpty_ShouldReturnTrue()
        {
            var logMock = new Mock<ILogger>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;
            var folderId = Guid.NewGuid();
            var namedIndexMock = new Mock<NamedIndex>("testindex2");
            var dir1 = new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", folderId));
            var dir2 = new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Ref", folderId));

            IndexingServiceSettings.NamedIndexElements.Add(namedIndexMock.Object.Name, new Configuration.NamedIndexElement() { Name = namedIndexMock.Object.Name });
            IndexingServiceSettings.MaxHitsForReferenceSearch = 1;
            IndexingServiceSettings.MaxHitsForReferenceSearch = 1;
            IndexingServiceSettings.NamedIndexDirectories.Add(namedIndexMock.Object.Name, Lucene.Net.Store.FSDirectory.Open(dir1));
            IndexingServiceSettings.ReferenceIndexDirectories.Add(namedIndexMock.Object.Name, Lucene.Net.Store.FSDirectory.Open(dir2));
            IndexingServiceSettings.MainDirectoryInfos.Add(namedIndexMock.Object.Name, dir1);
            IndexingServiceSettings.ReferenceDirectoryInfos.Add(namedIndexMock.Object.Name, dir2);

            var totalHits = 1;
            var docs = new Collection<ScoreDocument>
            {
                new ScoreDocument(new Document
            {
                new TextField(IndexingServiceSettings.TitleFieldName,"Title",Field.Store.YES),
                new TextField(IndexingServiceSettings.DisplayTextFieldName,"Body",Field.Store.YES),
                new TextField(IndexingServiceSettings.MetadataFieldName,"Meta",Field.Store.YES),
                new TextField(IndexingServiceSettings.NamedIndexFieldName,"testindex1",Field.Store.YES),
                new TextField(IndexingServiceSettings.IdFieldName,"1",Field.Store.YES),
                new TextField(IndexingServiceSettings.VirtualPathFieldName,"vp1",Field.Store.YES)
            }, 1)
            };

            _documentHelperMock.Setup(x => x.SingleIndexSearch(It.IsAny<string>(), It.IsAny<NamedIndex>(), It.IsAny<int>(), out totalHits)).Returns(docs);

            var classInstant = SetupMock();
            var result = classInstant.RemoveByVirtualPath("vp1");

            Assert.True(result);
        }
    }
}
