using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Cms.Shell.Search.Internal;
using EPiServer.Cms.Shell.UI.Test.Fakes;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Localization;
using EPiServer.Search;
using EPiServer.Search.Queries;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Shell.Search;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EPiServer.Cms.Shell.UI.Test.Search
{
    public class BlockSearchProviderTest
    {
        public BlockSearchProviderTest()
        {
            SetupTestSubject();
        }

        [Fact]
        public void Search_WithQueryWithoutSearchRoots_ShouldNotAddPathQuery()
        {
            var query = new Query("query");
            _searchProvider.Search(query);
            _searchHandlerMock
                .Verify(handler => handler.GetSearchResults(
                    It.Is<GroupQuery>(groupQuery => GetQueryExpressionsRecursive<VirtualPathQuery>(groupQuery).Count() == 0),
                    null, null, It.IsAny<int>(),
                    It.IsAny<int>()));
        }

        [Fact]
        public void Search_WithQueryContainingOneSearchRoot_ShouldAddCorrectGuidAsVirtualPathNode()
        {
            var query = new Query("query")
            {
                SearchRoots = new string[] { "1" }
            };

            _searchProvider.Search(query);
            _searchHandlerMock
                .Verify(handler => handler.GetSearchResults(
                    It.Is<GroupQuery>(groupQuery => GetQueryExpressionsRecursive<VirtualPathQuery>(groupQuery).FirstOrDefault().VirtualPathNodes.Contains(_contentGuid.ToString())),
                    null, null, It.IsAny<int>(),
                    It.IsAny<int>()));
        }

        [Fact]
        public void Search_WithQueryContainingMultipleSearchRoots_ShouldAddCorrectNumberOfVirtualPathQueries()
        {
            var query = new Query("query")
            {
                SearchRoots = new string[] { "1", "2", "3" }
            };

            _searchProvider.Search(query);
            _searchHandlerMock
                .Verify(handler => handler.GetSearchResults(
                    It.Is<GroupQuery>(groupQuery => GetQueryExpressionsRecursive<VirtualPathQuery>(groupQuery).Count() == 3),
                    null, null, It.IsAny<int>(),
                    It.IsAny<int>()));
        }

        [Fact]
        public void Search_WithSearchRootsThatCannotBeParsedToContentReference_ShouldNotAddPathQuery()
        {
            var query = new Query("query")
            {
                SearchRoots = new string[] { "not", "parsable", "to", "content", "reference" }
            };

            _searchProvider.Search(query);
            _searchHandlerMock
                .Verify(handler => handler.GetSearchResults(
                    It.Is<GroupQuery>(groupQuery => GetQueryExpressionsRecursive<VirtualPathQuery>(groupQuery).Count() == 0),
                    null, null, It.IsAny<int>(),
                    It.IsAny<int>()));
        }

        #region private members
        private Mock<SearchHandler> _searchHandlerMock;
        private BlockSearchProvider _searchProvider;
        private Guid _contentGuid;

        private IEnumerable<T> GetQueryExpressionsRecursive<T>(GroupQuery query) where T : IQueryExpression
        {
            var groupQueries = query.QueryExpressions.OfType<GroupQuery>();
            var queryList = new List<T>();
            foreach (var group in groupQueries)
            {
                var recursiveResult = GetQueryExpressionsRecursive<T>(group);
                if (recursiveResult.Any())
                {
                    queryList.AddRange(recursiveResult);
                }
            }
            queryList.AddRange(query.QueryExpressions.OfType<T>());
            return queryList;
        }

        private void SetupTestSubject()
        {
            _searchHandlerMock = new Mock<SearchHandler>(null, null, Options.Create<SearchOptions>(new SearchOptions()));
            var contentTypeRepositoryMock = new Mock<IContentTypeRepository<BlockType>>();
            var contentRepositoryMock = new Mock<IContentRepository>();
            var contentMock = new Mock<IContent>();
            _contentGuid = Guid.NewGuid();
            contentMock.Setup(mock => mock.ContentGuid).Returns(_contentGuid);
            contentRepositoryMock.Setup(mock => mock.Get<IContent>(It.IsAny<ContentReference>())).Returns(contentMock.Object);

            var contentSearchHandler = new Mock<ContentSearchHandler>();
            contentSearchHandler.Setup(h => h.GetVirtualPathNodes(It.IsAny<ContentReference>())).Returns(new List<string> { _contentGuid.ToString() });
            contentSearchHandler.Setup(h => h.GetContent<IContent>(It.IsAny<IndexResponseItem>())).Returns(contentMock.Object);

            var languageBranchRepositoryMock = new Mock<ILanguageBranchRepository>();
            languageBranchRepositoryMock
                .Setup(mock => mock.ListEnabled())
                .Returns(new List<LanguageBranch>());

            _searchProvider = new BlockSearchProvider(
                LocalizationService.Current,
                new Mock<ISiteDefinitionResolver>().Object,
                contentTypeRepositoryMock.Object,
                null,
                () => new FakeSiteDefinition(),
                contentRepositoryMock.Object,
                languageBranchRepositoryMock.Object,
                _searchHandlerMock.Object,
                contentSearchHandler.Object,
                new SearchIndexConfig(),
                null,
                Mock.Of<IContentLanguageAccessor>(),
                Mock.Of<UrlResolver>(),
                Mock.Of<TemplateResolver>(),
                Mock.Of<IBlobResolver>())
            {
                IsSearchActive = true,
                HasAdminAccess = () => true
            };
        }
        #endregion
    }
}
