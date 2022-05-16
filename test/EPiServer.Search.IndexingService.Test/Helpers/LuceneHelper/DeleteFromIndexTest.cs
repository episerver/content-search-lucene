using System;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(IndexingService.Helpers.LuceneHelper), nameof(IndexingService.Helpers.LuceneHelper.DeleteFromIndex))]
    public class DeleteFromIndexTest : LuceneHelperTestBase
    {
        [Fact]
        public void DeleteFromIndex_WhenHaveFileToDelete_ShouldReturnTrue()
        {
            var folderId = Guid.NewGuid().ToString();

            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", folderId)));
            var dir2 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Ref", folderId)));

            var iwc = new IndexWriterConfig(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.Analyzer);

            using (var writer = new IndexWriter(dir1, iwc))
            {
                var doc = new Document
                {
                    new TextField("F", "hello you", Field.Store.YES)
                };
                writer.AddDocument(doc);
            }

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);
            namedIndexMock.SetupGet(x => x.ReferenceDirectory).Returns(() => dir2);

            var classInstant = SetupMock();
            var result = classInstant.DeleteFromIndex(namedIndexMock.Object, "1", true);
            Assert.True(result);
        }

        [Fact]
        public void DeleteFromIndex_WhenHaveNoFile_ShouldReturnFalse()
        {
            var folderId = Guid.NewGuid().ToString();

            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", folderId)));
            var dir2 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Ref", folderId)));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);
            namedIndexMock.SetupGet(x => x.ReferenceDirectory).Returns(() => dir2);

            var classInstant = SetupMock();
            var result = classInstant.DeleteFromIndex(namedIndexMock.Object, "2", true);
            Assert.False(result);
        }
    }
}
