using System;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.DocumentHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper), nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper.OptimizeIndex))]
    public class OptimizeIndexTest : DocumentHelperTestBase
    {
        [Fact]
        public void OptimizeIndex_ShouldWork()
        {
            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);

            var classInstant = SetupMock();
            classInstant.OptimizeIndex(namedIndexMock.Object);
        }
    }
}
