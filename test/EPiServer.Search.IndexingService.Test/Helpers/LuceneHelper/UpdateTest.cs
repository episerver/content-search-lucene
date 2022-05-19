using System;
using System.Collections.ObjectModel;
using Lucene.Net.Documents;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(IndexingService.Helpers.LuceneHelper), nameof(IndexingService.Helpers.LuceneHelper.Update))]
    public class UpdateTest : LuceneHelperTestBase
    {
        [Fact]
        public void Update_ShouldWork()
        {
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

            var dir = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => dir);

            var doc = new Document
            {
                new TextField(IndexingServiceSettings.VirtualPathFieldName, "vp", Field.Store.YES)
            };
            _documentHelperMock.Setup(x => x.GetDocumentById(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(doc);
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.IsAny<string>())).Returns("something");
            _feedHelperMock.Setup(x => x.PrepareAuthors(It.IsAny<FeedItemModel>())).Returns("someone");

            var classInstant = SetupMock();
            classInstant.Update(feed, namedIndexMock.Object);
        }
    }
}
