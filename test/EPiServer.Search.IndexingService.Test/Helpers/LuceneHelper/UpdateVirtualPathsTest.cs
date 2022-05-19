using System;
using System.Collections.ObjectModel;
using Lucene.Net.Documents;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(IndexingService.Helpers.LuceneHelper), nameof(IndexingService.Helpers.LuceneHelper.UpdateVirtualPaths))]
    public class UpdateVirtualPathsTest : LuceneHelperTestBase
    {
        [Fact]
        public void UpdateVirtualPaths_WhenNewVirtualPathIsEmpty_ShouldReturnFalse()
        {
            var classInstant = SetupMock();
            var result = classInstant.UpdateVirtualPaths("vp", "");
            Assert.False(result);
        }

        [Fact]
        public void UpdateVirtualPaths_WhenNewVirtualPathEqualToOldVirtualPath_ShouldReturnFalse()
        {
            var classInstant = SetupMock();
            var result = classInstant.UpdateVirtualPaths("vp", "vp");
            Assert.False(result);
        }

        [Fact]
        public void UpdateVirtualPaths_ShouldReturnTrue()
        {
            var folderId = Guid.NewGuid();
            var namedIndexMock = new Mock<NamedIndex>("testindex1");
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
            var result = classInstant.UpdateVirtualPaths("vp1", "vp2");
            Assert.True(result);
        }
    }
}
