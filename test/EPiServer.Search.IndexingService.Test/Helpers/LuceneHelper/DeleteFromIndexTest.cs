using EPiServer.Logging.Compatibility;
using EPiServer.Search.IndexingService.Configuration;
using EPiServer.Search.IndexingService.Helpers;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.DeleteFromIndex))]
    public class DeleteFromIndexTest : LuceneHelperTestBase
    {
        [Fact]
        public void DeleteFromIndex_WhenHaveFileToDelete_ShouldReturnTrue()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index1\Main"));
            var dir2 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index1\Ref"));

            IndexWriterConfig iwc = new IndexWriterConfig(IndexingServiceSettings.LuceneVersion, IndexingServiceSettings.Analyzer);

            using (IndexWriter writer = new IndexWriter(dir1, iwc))
            {
                Document doc = new Document();
                doc.Add(new TextField("F", "hello you", Field.Store.YES));
                writer.AddDocument(doc);
            }

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);
            namedIndexMock.SetupGet(x => x.ReferenceDirectory).Returns(() => dir2);
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.Name, new ReaderWriterLockSlim());
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.ReferenceName, new ReaderWriterLockSlim());

            var classInstant = SetupMock();
            var result = classInstant.DeleteFromIndex(namedIndexMock.Object, "1", true);
            Assert.True(result);
        }

        [Fact]
        public void DeleteFromIndex_WhenHaveNoFile_ShouldReturnFalse()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index2\Main"));
            var dir2 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index2\Ref"));

            var namedIndexMock = new Mock<NamedIndex>("testindex2");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);
            namedIndexMock.SetupGet(x => x.ReferenceDirectory).Returns(() => dir2);
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.Name, new ReaderWriterLockSlim());
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.ReferenceName, new ReaderWriterLockSlim());

            var classInstant = SetupMock();
            var result = classInstant.DeleteFromIndex(namedIndexMock.Object, "2", true);
            Assert.False(result);
        }
    }
}
