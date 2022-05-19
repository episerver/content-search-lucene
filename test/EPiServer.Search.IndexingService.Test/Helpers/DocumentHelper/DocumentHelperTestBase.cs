using System;
using System.Collections.ObjectModel;
using System.Threading;
using EPiServer.Search.IndexingService.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPiServer.Search.IndexingService.Test.Helpers.DocumentHelper
{
    public class DocumentHelperTestBase
    {
        protected readonly Mock<IResponseExceptionHelper> _responseExceptionHelperMock;
        protected readonly Mock<ILogger<IndexingService.Helpers.DocumentHelper>> _loggerMock;
        public DocumentHelperTestBase()
        {
            IndexingServiceSettings.ReaderWriterLocks.TryAdd("testindex1", new ReaderWriterLockSlim());
            IndexingServiceSettings.ReaderWriterLocks.TryAdd("testindex2", new ReaderWriterLockSlim());
            _responseExceptionHelperMock = new Mock<IResponseExceptionHelper>();
            _loggerMock = new Mock<ILogger<IndexingService.Helpers.DocumentHelper>>();
        }

        public IndexingService.Helpers.DocumentHelper SetupMock()
        {
            return new EPiServer.Search.IndexingService.Helpers.DocumentHelper(
                _responseExceptionHelperMock.Object, _loggerMock.Object);
        }

        public void AddDocumentForTest(NamedIndex namedIndex, string itemId)
        {
            var _feedHelperMock = new Mock<IFeedHelper>();
            var _commonFuncMock = new Mock<ICommonFunc>();
            var _responseExceptionHelperMock = new Mock<IResponseExceptionHelper>();
            var _documentHelperMock = new Mock<IDocumentHelper>();
            var _loggerMock1 = new Mock<ILogger<IndexingService.Helpers.LuceneHelper>>();

            var _luceneHelper = new IndexingService.Helpers.LuceneHelper(
                _feedHelperMock.Object,
                _responseExceptionHelperMock.Object,
                _commonFuncMock.Object,
                _documentHelperMock.Object,
                _loggerMock1.Object);

            var feed = new FeedItemModel()
            {
                Id = itemId,
                Title = "Test",
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

            _documentHelperMock.Setup(x => x.DocumentExists(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(false);
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.IsAny<string>())).Returns("something");
            _feedHelperMock.Setup(x => x.PrepareAuthors(It.IsAny<FeedItemModel>())).Returns("someone");

            var result = _luceneHelper.Add(feed, namedIndex);
        }
        public void DeleteDocumentForTest(NamedIndex namedIndex, string itemId)
        {
            var _feedHelperMock = new Mock<IFeedHelper>();
            var _commonFuncMock = new Mock<ICommonFunc>();
            var _responseExceptionHelperMock = new Mock<IResponseExceptionHelper>();
            var _documentHelperMock = new Mock<IDocumentHelper>();
            var _loggerMock1 = new Mock<ILogger<IndexingService.Helpers.LuceneHelper>>();

            var _luceneHelper = new IndexingService.Helpers.LuceneHelper(
                _feedHelperMock.Object,
                _responseExceptionHelperMock.Object,
                _commonFuncMock.Object,
                _documentHelperMock.Object,
                _loggerMock1.Object);

            var result = _luceneHelper.DeleteFromIndex(namedIndex, itemId, false);
        }
    }
}
