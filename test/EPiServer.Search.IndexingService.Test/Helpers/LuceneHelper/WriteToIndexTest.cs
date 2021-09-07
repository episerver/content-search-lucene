using EPiServer.Logging.Compatibility;
using Lucene.Net.Documents;
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
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.WriteToIndex))]
    public class WriteToIndexTest:LuceneHelperTestBase
    {
        [Fact]
        public void WriteToIndexTest_WhenDocumentExists_ShouldReturnFalse()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;
            var doc = new Document();
            var namedIndex = new NamedIndex("testindex1");
            _documentHelperMock.Setup(x => x.DocumentExists(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(true);

            var classInstant = SetupMock();
            var result = classInstant.WriteToIndex("1", doc, namedIndex);

            Assert.False(result);
        }

        [Fact]
        public void WriteToIndexTest_WhenDocumentNotExists_ShouldReturnTrue()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;
            
            var doc = new Document();
            var dir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index"));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir);
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.Name, new ReaderWriterLockSlim());

            _documentHelperMock.Setup(x => x.DocumentExists(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(false);

            var classInstant = SetupMock();
            var result = classInstant.WriteToIndex("1", doc, namedIndexMock.Object);

            Assert.True(result);
        }
    }
}
