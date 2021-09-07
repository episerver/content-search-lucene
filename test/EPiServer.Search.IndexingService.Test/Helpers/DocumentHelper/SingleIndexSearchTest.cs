using EPiServer.Logging.Compatibility;
using EPiServer.Search.IndexingService.Helpers;
using Lucene.Net.Index;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.DocumentHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper), nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper.SingleIndexSearch))]
    public class SingleIndexSearchTest : DocumentHelperTestBase
    {
        [Fact]
        public void SingleIndexSearch_ShouldHaveOneResult()
        {
            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);

            var itemId = Guid.NewGuid().ToString();
            AddDocumentForTest(namedIndexMock.Object, itemId);

            int totalHits = 0;
            var classInstant = SetupMock();
            var result = classInstant.SingleIndexSearch("EPISERVER_SEARCH_ID:"+ itemId, namedIndexMock.Object, 1, out totalHits);
            Assert.Single(result);

            DeleteDocumentForTest(namedIndexMock.Object, itemId);
        }
    }
}
