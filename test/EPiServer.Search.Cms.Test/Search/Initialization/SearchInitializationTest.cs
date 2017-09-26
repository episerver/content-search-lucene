using System;
using System.Collections.Generic;
using System.Globalization;
using EPiServer.Core;
using Moq;
using Xunit;

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

            searchHandler.Verify(s => s.UpdateItem(It.IsAny<IContent>()), Times.Between(2,2, Range.Inclusive));
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

            searchHandler.Verify(s => s.UpdateItem(It.IsAny<IContent>()), Times.Never);
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
