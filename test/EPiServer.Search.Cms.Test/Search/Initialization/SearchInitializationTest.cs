using System;
using System.Collections.Generic;
using System.Globalization;
using EPiServer.Core;
using Moq;
using Xunit;
using EPiServer.DataAbstraction;

namespace EPiServer.Search.Initialization
{
    public class SearchInitializationTest
    {
        [Fact]
        public void SearchEventHandler_WhenSecurityChangedForNoneVersionableContent_ShouldUpdateItem()
        {
            var link = new ContentReference() { ID = 5 };
            var searchContent = new NoneVersionableSearchContent() { ContentLink = link };

            Mock<IContentRepository> contentRep = new Mock<IContentRepository>();
            contentRep.Setup(s => s.Get<IContent>(link)).Returns(searchContent);
            contentRep.Setup(s => s.GetLanguageBranches<IContent>(link)).Returns(new IContent[] { searchContent });

            Mock<ContentSearchHandler> searchHandler = new Mock<ContentSearchHandler>();
            var subject = new SearchInitialization.SearchEventHandler(searchHandler.Object, contentRep.Object);

            subject.ContentSecurityRepository_Saved(null, new EPiServer.DataAbstraction.ContentSecurityEventArg(link, null, EPiServer.Security.SecuritySaveType.None));

            searchHandler.Verify(s => s.UpdateItem(It.IsAny<IContent>()), Times.Once());
        }


        [Fact]
        public void EventHandler_WhenSecurityChangedForVersionableContentWithLanguages_ShouldUpdateItem()
        {
            var link = new ContentReference() { ID = 5 };
            var engSearchContent = new SearchContent() { ContentLink = link , Language = CultureInfo.GetCultureInfo("en")};
            var svSearchContent = new SearchContent() { ContentLink = link, Language = CultureInfo.GetCultureInfo("sv") };

            Mock<IContentRepository> contentRep = new Mock<IContentRepository>();
            contentRep.Setup(s => s.Get<IContent>(link)).Returns(engSearchContent);
            contentRep.Setup(s => s.GetLanguageBranches<IContent>(link)).Returns(new IContent[] { engSearchContent, svSearchContent });


            Mock<ContentSearchHandler> searchHandler = new Mock<ContentSearchHandler>();
            var subject = new SearchInitialization.SearchEventHandler(searchHandler.Object, contentRep.Object);

            subject.ContentSecurityRepository_Saved(null, new EPiServer.DataAbstraction.ContentSecurityEventArg(link, null, EPiServer.Security.SecuritySaveType.None));

            searchHandler.Verify(s => s.UpdateItem(It.IsAny<IContent>()), Times.Exactly(2));
        }


        [Fact]
        public void EventHandler_WhenSecurityChangedForVersionableContentAndTheVersionIsNotPublished_ShouldNotUpdateItem()
        {
            var link = new ContentReference() { ID = 5 };
            var searchContent = new SearchContent() { ContentLink = link };
            searchContent.IsPendingPublish = true;

            Mock<IContentRepository> contentRep = new Mock<IContentRepository>();
            contentRep.Setup(s => s.Get<IContent>(link)).Returns(searchContent);

            Mock<ContentSearchHandler> searchHandler = new Mock<ContentSearchHandler>();
            var subject = new SearchInitialization.SearchEventHandler(searchHandler.Object, contentRep.Object);

            subject.ContentSecurityRepository_Saved(null, new EPiServer.DataAbstraction.ContentSecurityEventArg(link, null, EPiServer.Security.SecuritySaveType.None));

            searchHandler.Verify(s => s.UpdateItem(It.IsAny<IContent>()), Times.Never());
        }

        [Fact]
        public void EventHandler_WhenPageTypesAreConverted_ShouldUpdateConvertedItem()
        {
            var pageLink = new PageReference(5);
            var content = Mock.Of<IContent>();

            var contentRepository = new Mock<IContentRepository>();
            contentRepository.Setup(s => s.TryGet(pageLink, out content)).Returns(true);

            var searchHandler = new Mock<ContentSearchHandler>();
            var subject = new SearchInitialization.SearchEventHandler(searchHandler.Object, contentRepository.Object);

            subject.PageTypeConverter_PagesConverted(null, new ConvertedPageEventArgs(pageLink, null, null, false));

            searchHandler.Verify(s => s.UpdateItem(content), Times.Once());
        }

        [Fact]
        public void EventHandler_WhenPageTypesAreConvertedButNoPageWasFound__ShouldNotCallUpdateItem()
        {
            var pageLink = new PageReference(5);

            var contentRepository = new Mock<IContentRepository>();
            IContent content = null;
            contentRepository.Setup(s => s.TryGet(pageLink, out content)).Returns(false);

            var searchHandler = new Mock<ContentSearchHandler>();
            var subject = new SearchInitialization.SearchEventHandler(searchHandler.Object, contentRepository.Object);

            subject.PageTypeConverter_PagesConverted(null, new ConvertedPageEventArgs(pageLink, null, null, false));

            searchHandler.Verify(s => s.UpdateItem(It.IsAny<IContent>()), Times.Never());
        }

        [Fact]
        public void EventHandler_WhenPageTypesAreConvertedRecursively_ShouldUpdateDescendantPagesOfSameType()
        {
            var newPageType = new PageType { ID = 44 };
            var pageLink = new PageReference(5);
            IContent content = new SearchContent { ContentTypeID = newPageType.ID, ContentLink = new ContentReference(100) };
            IContent childA = new SearchContent { ContentTypeID = newPageType.ID, ContentLink = new ContentReference(200) };
            IContent childB = new SearchContent { ContentTypeID = 12, ContentLink = new ContentReference(300) };
            IContent childC = new SearchContent { ContentTypeID = newPageType.ID, ContentLink = new ContentReference(400) };

            var contentRepository = new Mock<IContentRepository>();
            contentRepository.Setup(s => s.TryGet(pageLink, out content)).Returns(true);
            contentRepository.Setup(s => s.GetChildren<IContent>(pageLink)).Returns(new[] { childA, childB });
            contentRepository.Setup(s => s.GetChildren<IContent>(childB.ContentLink)).Returns(new[] { childC });

            var searchHandler = new Mock<ContentSearchHandler>();
            var subject = new SearchInitialization.SearchEventHandler(searchHandler.Object, contentRepository.Object);

            subject.PageTypeConverter_PagesConverted(null, new ConvertedPageEventArgs(pageLink, null, newPageType, true));

            searchHandler.Verify(s => s.UpdateItem(content), Times.Once());
            searchHandler.Verify(s => s.UpdateItem(childA), Times.Once());
            searchHandler.Verify(s => s.UpdateItem(childB), Times.Never());
            searchHandler.Verify(s => s.UpdateItem(childC), Times.Once());
        }
    }

    internal class SearchContent : ContentBase, ILocalizable
    {
        public IEnumerable<CultureInfo> ExistingLanguages
        {
            get;set;
        }

        public CultureInfo Language
        {
            get; set;
        }

        public CultureInfo MasterLanguage
        {
            get; set;
        }
    }

    internal class NoneVersionableSearchContent : IContent
    {
        public Guid ContentGuid { get; set; }

        public ContentReference ContentLink { get; set; }

        public int ContentTypeID { get; set; }

        public bool IsDeleted { get; set; }

        public string Name { get; set; }

        public ContentReference ParentLink { get; set; }

        public PropertyDataCollection Property { get; set; }
    }
}
