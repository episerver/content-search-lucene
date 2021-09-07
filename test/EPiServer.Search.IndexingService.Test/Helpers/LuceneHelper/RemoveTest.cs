using EPiServer.Logging.Compatibility;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.Remove))]
    public class RemoveTest : LuceneHelperTestBase
    {
        [Fact]
        public void RemoveFeedItemModelNamedIndex_WhenFeedItemIdNotEqualIgnoreItemId_ShouldRunRemove()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;
            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index1\Main"));
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.Name, new ReaderWriterLockSlim());

            var classInstant = SetupMock();
            classInstant.Remove(new FeedItemModel() { Id="1" }, namedIndexMock.Object);
        }

        [Fact]
        public void RemoveFeedItemModelNamedIndex_WhenFeedItemIdNotEqualIgnoreItemId_ShouldRunGetAutoUpdateVirtualPathValue()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;
            var namedIndexMock = new Mock<NamedIndex>("testindex2");
            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index1\Main"));
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.Name, new ReaderWriterLockSlim());

            _feedHelperMock.Setup(x => x.GetAutoUpdateVirtualPathValue(It.IsAny<FeedItemModel>())).Returns(true);

            var feed = new FeedItemModel() { Id = "1" };
            feed.ElementExtensions.Add(IndexingServiceSettings.SyndicationItemElementNameVirtualPath, new Collection<string>() {  });

            var classInstant = SetupMock();
            classInstant.Remove(feed, namedIndexMock.Object);
        }

        [Fact]
        public void RemoveStringNamedIndexBool_ShouldWork()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            var namedIndexMock = new Mock<NamedIndex>("testindex3");
            var dir1 = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index1\Main"));
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir1);
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.Name, new ReaderWriterLockSlim());

            var classInstant = SetupMock();
            classInstant.Remove("1", namedIndexMock.Object,false);
        }
    }
}
