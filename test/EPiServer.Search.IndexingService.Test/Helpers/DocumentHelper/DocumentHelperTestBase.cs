using EPiServer.Search.IndexingService.Helpers;
using log4net;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService.Test.Helpers.DocumentHelper
{
    public class DocumentHelperTestBase
    {
        protected readonly Mock<IResponseExceptionHelper> _responseExceptionHelperMock;
        public DocumentHelperTestBase()
        {
            _responseExceptionHelperMock = new Mock<IResponseExceptionHelper>();

            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

        }

        public EPiServer.Search.IndexingService.Helpers.DocumentHelper SetupMock()
        {
            return new EPiServer.Search.IndexingService.Helpers.DocumentHelper(
                _responseExceptionHelperMock.Object);
        }

        public void AddDocumentForTest(NamedIndex namedIndex, string itemId)
        {
            Mock<IFeedHelper> _feedHelperMock = new Mock<IFeedHelper>();
            Mock<ICommonFunc> _commonFuncMock = new Mock<ICommonFunc>();
            Mock<IResponseExceptionHelper> _responseExceptionHelperMock = new Mock<IResponseExceptionHelper>();
            Mock<IDocumentHelper> _documentHelperMock = new Mock<IDocumentHelper>();

            var _luceneHelper = new EPiServer.Search.IndexingService.Helpers.LuceneHelper(
                _feedHelperMock.Object,
                _responseExceptionHelperMock.Object,
                _commonFuncMock.Object,
                _documentHelperMock.Object);

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
            Mock<IFeedHelper> _feedHelperMock = new Mock<IFeedHelper>();
            Mock<ICommonFunc> _commonFuncMock = new Mock<ICommonFunc>();
            Mock<IResponseExceptionHelper> _responseExceptionHelperMock = new Mock<IResponseExceptionHelper>();
            Mock<IDocumentHelper> _documentHelperMock = new Mock<IDocumentHelper>();

            var _luceneHelper = new EPiServer.Search.IndexingService.Helpers.LuceneHelper(
                _feedHelperMock.Object,
                _responseExceptionHelperMock.Object,
                _commonFuncMock.Object,
                _documentHelperMock.Object);

            var result = _luceneHelper.DeleteFromIndex(namedIndex, itemId, false);
        }
    }
}
