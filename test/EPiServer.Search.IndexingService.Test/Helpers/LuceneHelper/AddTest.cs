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
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.Add))]
    public class AddTest : LuceneHelperTestBase
    {
        [Fact]
        public void Add_WhenItemIsNull_ShouldReturnFalse()
        {
            var classItant = SetupMock();
            var result = classItant.Add(null, It.IsAny<NamedIndex>());

            Assert.False(result);
        }

        [Fact]
        public void Add_WhenItemIdIsEmpty_ShouldReturnFalse()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;
            var item = new FeedItemModel
            {
                Id = ""
            };
            var namedIndex = new NamedIndex("testindex1");

            var classItant = SetupMock();
            var result = classItant.Add(item, namedIndex);

            Assert.False(result);
        }

        [Fact]
        public void Add_WhenDocumentExists_ShouldReturnFalse()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;
            var item = new FeedItemModel
            {
                Id = "1"
            };
            var namedIndex = new NamedIndex("testindex1");
            _documentHelperMock.Setup(x => x.DocumentExists(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(true);

            var classItant = SetupMock();
            var result = classItant.Add(item, namedIndex);

            Assert.False(result);
        }

        [Fact]
        public void Add_WhenEverythingIsValid_ShouldReturnTrue()
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

            var dir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index"));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir);
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.Name, new ReaderWriterLockSlim());

            _documentHelperMock.Setup(x => x.DocumentExists(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(false);
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.IsAny<string>())).Returns("something");
            _feedHelperMock.Setup(x => x.PrepareAuthors(It.IsAny<FeedItemModel>())).Returns("someone");

            var classItant = SetupMock();
            var result = classItant.Add(feed, namedIndexMock.Object);

            Assert.True(result);
        }
    }
}
