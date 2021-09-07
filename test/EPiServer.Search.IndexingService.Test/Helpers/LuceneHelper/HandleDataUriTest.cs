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
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.HandleDataUri))]
    public class HandleDataUriTest : LuceneHelperTestBase
    {
        [Fact]
        public void HandleDataUri_WhenUriStringIsNotUri_ShouldReturnFalse()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            var feed = new FeedItemModel();

            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns((string)null);

            var classInstant = SetupMock();
            var result = classInstant.HandleDataUri(feed, new NamedIndex("testindex1"));
            Assert.False(result);
        }

        [Fact]
        public void HandleDataUri_WhenFileNotExist_ShouldReturnFalse()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            var feed = new FeedItemModel();
            
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns(@"c:\fake\App_Data\Index\file.txt");

            var classInstant = SetupMock();
            var result = classInstant.HandleDataUri(feed, new NamedIndex("testindex1"));
            Assert.False(result);
        }

        [Fact]
        public void HandleDataUri_WhenUriIsFileAndRunAdd_ShouldReturnTrue()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            var feed = new FeedItemModel()
            {
                Id = "Id",
                Title = "Header test",
                DisplayText = "Body test",
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Uri = new Uri("http://www.google.com"),
                Culture = "sv-SE"
            };
            feed.Authors.Add("Author1");
            feed.Authors.Add("Author2");
            feed.Categories.Add("Category1");
            feed.Categories.Add("Category2");
            feed.ElementExtensions.Add(IndexingServiceSettings.SyndicationItemElementNameAcl, new Collection<string>() { "group1", "group2" });
            feed.ElementExtensions.Add(IndexingServiceSettings.SyndicationItemElementNameVirtualPath, new Collection<string>() { "vp1", "vp2" });
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameBoostFactor, "1");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameCulture, "sv");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameType, "EPiServer.Search.IndexItem, EPiServer.Search");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameReferenceId, "1");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemElementNameMetadata, "Metadata");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameItemStatus, "1");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNamePublicationStart, DateTime.Now.ToString());
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNamePublicationEnd, DateTime.Now.AddDays(1).ToString());
            

            _commonFuncMock.Setup(x => x.GetFileUriContent(It.IsAny<Uri>())).Returns("sometext");
            
            var dir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index"));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir);
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.Name, new ReaderWriterLockSlim());

            _feedHelperMock.Setup(x => x.PrepareAuthors(It.IsAny<FeedItemModel>())).Returns("someone");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.IsAny<string>())).Returns("something");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s=>s== IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns(@"c:\fake\App_Data\Index\segments.gen");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameIndexAction))).Returns("add");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameReferenceId))).Returns("testindex1_ref");
            

            var classInstant = SetupMock();
            var result = classInstant.HandleDataUri(feed, namedIndexMock.Object);
            Assert.True(result);
        }

        [Fact]
        public void HandleDataUri_WhenUriIsFileAndRunUpdate_ShouldReturnTrue()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            var feed = new FeedItemModel()
            {
                Id = "Id",
                Title = "Header test",
                DisplayText = "Body test",
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Uri = new Uri("http://www.google.com"),
                Culture = "sv-SE"
            };
            feed.Authors.Add("Author1");
            feed.Authors.Add("Author2");
            feed.Categories.Add("Category1");
            feed.Categories.Add("Category2");
            feed.ElementExtensions.Add(IndexingServiceSettings.SyndicationItemElementNameAcl, new Collection<string>() { "group1", "group2" });
            feed.ElementExtensions.Add(IndexingServiceSettings.SyndicationItemElementNameVirtualPath, new Collection<string>() { "vp1", "vp2" });
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameBoostFactor, "1");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameCulture, "sv");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameType, "EPiServer.Search.IndexItem, EPiServer.Search");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameReferenceId, "1");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemElementNameMetadata, "Metadata");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameItemStatus, "1");
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNamePublicationStart, DateTime.Now.ToString());
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNamePublicationEnd, DateTime.Now.AddDays(1).ToString());


            _commonFuncMock.Setup(x => x.GetFileUriContent(It.IsAny<Uri>())).Returns("sometext");

            var dir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index"));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir);
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.Name, new ReaderWriterLockSlim());

            _feedHelperMock.Setup(x => x.PrepareAuthors(It.IsAny<FeedItemModel>())).Returns("someone");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.IsAny<string>())).Returns("something");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns(@"c:\fake\App_Data\Index\segments.gen");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameIndexAction))).Returns("update");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameReferenceId))).Returns("testindex1_ref");


            var classInstant = SetupMock();
            var result = classInstant.HandleDataUri(feed, namedIndexMock.Object);
            Assert.True(result);
        }
    }
}
