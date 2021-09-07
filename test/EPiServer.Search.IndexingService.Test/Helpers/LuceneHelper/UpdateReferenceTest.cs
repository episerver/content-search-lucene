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
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.UpdateReference))]
    public class UpdateReferenceTest:LuceneHelperTestBase
    {
        [Fact]
        public void UpdateReference_WhenMainDocIsNull_ShouldReturnFalse()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            _documentHelperMock.Setup(x => x.GetDocumentById(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns((Document)null);

            var classInstant = SetupMock();
            var result = classInstant.UpdateReference("1", "2", new NamedIndex("testindex1"));
            Assert.False(result);
        }

        [Fact]
        public void UpdateReference_WhenMainDocIsNotNull_ShouldReturnTrue()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            var doc = new Document();
            doc.Add(new TextField(IndexingServiceSettings.VirtualPathFieldName, "vp", Field.Store.YES));
            _documentHelperMock.Setup(x => x.GetDocumentById(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(doc);

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.Name, new ReaderWriterLockSlim());

            var classInstant = SetupMock();
            var result = classInstant.UpdateReference("1", "2", new NamedIndex("testindex1"));
            Assert.True(result);
        }
    }
}
