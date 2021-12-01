using System;
using System.Collections.ObjectModel;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.DocumentHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper), nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper.MultiIndexSearch))]
    public class MultiIndexSearchTest : DocumentHelperTestBase
    {
        [Fact]
        public void MultiIndexSearch_ShouldHaveTwoResult()
        {
            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);


            var dir2 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));
            var namedIndexMock2 = new Mock<NamedIndex>("testindex2");
            namedIndexMock2.SetupGet(x => x.Directory).Returns(() => dir2);

            var itemId = Guid.NewGuid().ToString();
            var itemId2 = Guid.NewGuid().ToString();
            var collectionName = new Collection<NamedIndex>
            {
                namedIndexMock.Object,
                namedIndexMock2.Object
            };

            AddDocumentForTest(namedIndexMock.Object, itemId);
            AddDocumentForTest(namedIndexMock2.Object, itemId2);

            var classInstant = SetupMock();
            var result = classInstant.MultiIndexSearch("EPISERVER_SEARCH_TITLE:Test", collectionName, 2, out var totalHits);
            Assert.Equal(2, result.Count);

            DeleteDocumentForTest(namedIndexMock.Object, itemId);
            DeleteDocumentForTest(namedIndexMock2.Object, itemId2);
        }
    }
}
