using EPiServer.Logging.Compatibility;
using Lucene.Net.Documents;
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
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.Update))]
    public class UpdateTest : LuceneHelperTestBase
    {
        [Fact]
        public void Update_ShouldWork()
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

            var doc = new Document();
            doc.Add(new TextField(IndexingServiceSettings.VirtualPathFieldName, "vp", Field.Store.YES));
            _documentHelperMock.Setup(x => x.GetDocumentById(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(doc);
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.IsAny<string>())).Returns("something");
            _feedHelperMock.Setup(x => x.PrepareAuthors(It.IsAny<FeedItemModel>())).Returns("someone");

            var classInstant = SetupMock();
            classInstant.Update(feed, namedIndexMock.Object);
        }
    }
}
