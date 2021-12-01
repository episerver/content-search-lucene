using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.IndexingServiceHandler
{
    [Trait(nameof(EPiServer.Search.IndexingService.IndexingServiceHandler), nameof(EPiServer.Search.IndexingService.IndexingServiceHandler.UpdateIndex))]
    public class UpdateIndexTest : IndexingServiceHandlerTestBase
    {
        private FeedItemModel feedItem;
        private FeedModel feed;

        [Fact]
        public void UpdateIndex_WhenDataUriNotNull_ShouldAddToQueue()
        {
            Setup();

            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameReferenceId))).Returns("");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameIndexAction))).Returns("update");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns("http://www.google.com");

            var classInstant = SetupMock();
            classInstant.UpdateIndex(feed);
        }

        [Fact]
        public void UpdateIndex_WhenDataUriIsNullAndIndexActionIsAdd_ShouldRunAdd()
        {
            Setup();

            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameReferenceId))).Returns(Guid.NewGuid().ToString());
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameIndexAction))).Returns("add");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns("");

            var classInstant = SetupMock();
            classInstant.UpdateIndex(feed);
        }

        [Fact]
        public void UpdateIndex_WhenDataUriIsNullAndIndexActionIsUpdate_ShouldRunUpdate()
        {
            Setup();

            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameReferenceId))).Returns(Guid.NewGuid().ToString());
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameIndexAction))).Returns("update");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns("");

            var classInstant = SetupMock();
            classInstant.UpdateIndex(feed);
        }

        [Fact]
        public void UpdateIndex_WhenDataUriIsNullAndIndexActionIsRemove_ShouldRunRemove()
        {
            Setup();

            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameReferenceId))).Returns(Guid.NewGuid().ToString());
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameIndexAction))).Returns("remove");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns("");

            var classInstant = SetupMock();
            classInstant.UpdateIndex(feed);
        }

        private void Setup()
        {
            var itemId = Guid.NewGuid().ToString();
            feed = new FeedModel();
            feedItem = new FeedItemModel()
            {
                Id = itemId,
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

            feed.Items = new List<FeedItemModel>() { feedItem };

            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.IsAny<string>())).Returns("something");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameNamedIndex))).Returns("default");


            _commonFuncMock.Setup(x => x.IsValidIndex(It.IsAny<string>())).Returns(true);
            _commonFuncMock.Setup(x => x.IsModifyIndex(It.IsAny<string>())).Returns(true);

            _luceneHelperMock.Setup(x => x.GetReferenceIdForItem(It.IsAny<string>(), It.IsAny<NamedIndex>())).Returns(Guid.NewGuid().ToString());
        }
    }
}
