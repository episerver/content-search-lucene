using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.IndexingServiceHandler
{
    [Trait(nameof(EPiServer.Search.IndexingService.IndexingServiceHandler), nameof(EPiServer.Search.IndexingService.IndexingServiceHandler.GetSearchResults))]
    public class GetSearchResultsTest : IndexingServiceHandlerTestBase
    {
        [Fact]
        public void GetSearchResults_WhenNamedIndexIsInValid_ShouldThrowExeption()
        {
            var namedIndexNames = new string[] { "default" };

            var namedIndexMock = new Mock<NamedIndex>();
            //namedIndexMock.SetupGet(x => x.IsValid).Returns(()=>false);

            _responseExceptionHelperMock.Setup(x => x.HandleServiceError(It.IsAny<string>())).Throws(new HttpResponseException() { Status = 500 });

            var classInstant = SetupMock();
            var caughtException = Assert.Throws<HttpResponseException>(() => classInstant.GetSearchResults("",namedIndexNames,0,1));
            Assert.Equal(500, caughtException.Status);
        }

        [Fact]
        public void GetSearchResults_WhenNamedIndexNamesNotNull_ShouldReturnFeed()
        {
            var namedIndexNames = new string[] { "default" };
            var folderId = Guid.NewGuid();
            var namedIndexMock = new Mock<NamedIndex>("default");
            var dir1 = new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", folderId));
            var dir2 = new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Ref", folderId));

            IndexingServiceSettings.NamedIndexElements.Add(namedIndexMock.Object.Name, new Configuration.NamedIndexElement() { Name = namedIndexMock.Object.Name });
            IndexingServiceSettings.NamedIndexDirectories.Add(namedIndexMock.Object.Name, Lucene.Net.Store.FSDirectory.Open(dir1));
            IndexingServiceSettings.ReferenceIndexDirectories.Add(namedIndexMock.Object.Name, Lucene.Net.Store.FSDirectory.Open(dir2));
            IndexingServiceSettings.MainDirectoryInfos.Add(namedIndexMock.Object.Name, dir1);
            IndexingServiceSettings.ReferenceDirectoryInfos.Add(namedIndexMock.Object.Name, dir2);

            int totalHits = 0;
            Collection<ScoreDocument> docs = new Collection<ScoreDocument>() { new ScoreDocument(new Lucene.Net.Documents.Document(),1) };
            _luceneHelperMock.Setup(x => x.GetScoreDocuments(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<Collection<NamedIndex>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                out totalHits)).Returns(docs);
            
            var feedItem = new FeedItemModel()
            {
                Id = "1",
                Title = "Test",
                DisplayText = "Body test",
                Created = DateTime.Now,
                Modified = DateTime.Now,
                Uri = new Uri("http://www.google.com"),
                Culture = "sv-SE"
            };
            feedItem.Authors.Add("Author1");
            feedItem.Authors.Add("Author2");
            feedItem.Categories.Add("Category1");
            feedItem.Categories.Add("Category2");
            feedItem.ElementExtensions.Add(IndexingServiceSettings.SyndicationItemElementNameAcl, new Collection<string>() { "group1", "group2" });
            feedItem.ElementExtensions.Add(IndexingServiceSettings.SyndicationItemElementNameVirtualPath, new Collection<string>() { "vp1", "vp2" });
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameBoostFactor, "1");
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameCulture, "sv");
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameType, "EPiServer.Search.IndexItem, EPiServer.Search");
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameReferenceId, "1");
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemElementNameMetadata, "Metadata");
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNameItemStatus, "1");
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNamePublicationStart, DateTime.Now.ToString());
            feedItem.AttributeExtensions.Add(IndexingServiceSettings.SyndicationItemAttributeNamePublicationEnd, DateTime.Now.AddDays(1).ToString());

            _documentHelperMock.Setup(x => x.GetSyndicationItemFromDocument(It.IsAny<ScoreDocument>())).Returns(feedItem);

            var classInstant = SetupMock();
            var result = classInstant.GetSearchResults(
                string.Format("{0}:{1}", IndexingServiceSettings.IdFieldName, 1),
                namedIndexNames,
                0, 1);
            Assert.Equal("Test", result.Items.ToList()[0].Title);
        }
    }
}
