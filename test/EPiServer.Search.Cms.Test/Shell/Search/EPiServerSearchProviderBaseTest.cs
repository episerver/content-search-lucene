using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using EPiServer.Cms.Shell.Search;
using EPiServer.Cms.Shell.Search.Internal;
using EPiServer.Cms.Shell.UI.Test.Fakes;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Localization;
using EPiServer.Search;
using EPiServer.Search.Queries;
using EPiServer.Search.Queries.Lucene;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace EPiServer.Cms.Shell.UI.Test.Search
{
    public class EPiServerSearchProviderBaseTest
    {
        private readonly Mock<SearchHandler> _searchHandler;
        private readonly Mock<IContentRepository> _contentRepository;
        private readonly FakeSearchProvider _searchProvider;
        private readonly Mock<ILanguageBranchRepository> _languageBranchRespository;
        private readonly ContentSearchHandler _contentSearchHandler;
        private readonly Mock<IContentTypeRepository> _contentTypeRepository;
        private readonly Mock<ISiteDefinitionResolver> _siteDefinitionResolver;

        public EPiServerSearchProviderBaseTest()
        {
            _searchHandler = new Mock<SearchHandler>(null, null, Options.Create(new SearchOptions()));
            _contentTypeRepository = new Mock<IContentTypeRepository>();

            _contentRepository = new Mock<IContentRepository>();
            _contentSearchHandler = new Mock<ContentSearchHandler>().Object;

            _languageBranchRespository = new Mock<ILanguageBranchRepository>();
            _languageBranchRespository.Setup(r => r.ListEnabled()).Returns(new List<LanguageBranch>());

            _siteDefinitionResolver = new Mock<ISiteDefinitionResolver>();
            _siteDefinitionResolver.Setup(s => s.GetByContent(It.IsAny<ContentReference>(), It.IsAny<bool>())).Returns(new SiteDefinition());

            _searchProvider = new FakeSearchProvider(
                LocalizationService.Current,
                _siteDefinitionResolver.Object,
                _contentTypeRepository.Object,
                _contentRepository.Object,
                _languageBranchRespository.Object,
                _searchHandler.Object,
                () => new FakeSiteDefinition(),
                _contentSearchHandler,
                new SearchIndexConfig(),
                new UIDescriptorRegistry(new[]
                {
                    new UIDescriptor(typeof (IContentData)),
                    new UIDescriptor(typeof (IContentImage)),
                    new UIDescriptor(typeof (IContentVideo)),
                    new UIDescriptor(typeof (YouTubeVideo))
                }, null),
                Mock.Of<IContentLanguageAccessor>(),
                Mock.Of<UrlResolver>(),
                Mock.Of<TemplateResolver>())
            {
                HasAdminAccess = () => true,
                IsSearchActive = true
            };
        }

        [Fact]
        public void WhenTheContentDoesNotImplement_ILocalizable_ThenTheLanguageCodeShouldNotBePassedToEditPath()
        {
            var content = new Mock<IContent>();
            content.Setup(c => c.Name).Returns("My name");
            content.Setup(c => c.ContentLink).Returns(new ContentReference(2));
            content.Setup(c => c.ParentLink).Returns(new ContentReference(1));
            content.Setup(c => c.Property).Returns(new PropertyDataCollection());

            _contentRepository.Setup(r => r.Get<IContent>(It.IsAny<Guid>(), It.IsAny<LoaderOptions>())).Returns(content.Object);

            _searchProvider.EditPath = (contentData, contentLink, languageName) =>
            {
                Assert.Equal(string.Empty, languageName);
                return string.Empty;
            };

            var searchResults = new SearchResults();
            searchResults.IndexResponseItems.Add(new IndexResponseItem(Guid.NewGuid().ToString()));
            _searchHandler.Setup(s => s.GetSearchResults(It.IsAny<IQueryExpression>(), It.IsAny<string>(), It.IsAny<Collection<string>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(searchResults);

            _searchProvider.Search(new Query("Todd"));
        }

        [Fact]
        public void WhenTheContentDoesImplement_ILocalizable_ThenTheLanguageCodeShouldBePassedToEditPath()
        {
            var content = new Mock<IContent>();
            content.Setup(c => c.Name).Returns("My name");
            content.Setup(c => c.ContentLink).Returns(new ContentReference(2));
            content.Setup(c => c.ParentLink).Returns(new ContentReference(1));
            content.Setup(c => c.Property).Returns(new PropertyDataCollection());

            content.As<ILocalizable>().Setup(l => l.Language).Returns(new CultureInfo("en"));

            _contentRepository.Setup(r => r.Get<IContent>(It.IsAny<Guid>(), It.IsAny<LoaderOptions>())).Returns(content.Object);

            _searchProvider.EditPath = (contentData, contentLink, languageName) =>
            {
                Assert.Equal("en", languageName);
                return string.Empty;
            };

            var searchResults = new SearchResults();
            searchResults.IndexResponseItems.Add(new IndexResponseItem(Guid.NewGuid().ToString()));
            _searchHandler.Setup(s => s.GetSearchResults(It.IsAny<IQueryExpression>(), It.IsAny<string>(), It.IsAny<Collection<string>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(searchResults);

            _searchProvider.Search(new Query("Todd"));
        }

        [Fact]
        public void When_allowedTypes_are_passed_as_params_these_should_be_added_to_the_query_expression()
        {
            _contentTypeRepository.Setup(x => x.List()).Returns(new[] { new ContentType { ModelType = typeof(ImageData) }, new ContentType { ModelType = typeof(VideoData) } });

            Func<GroupQuery, bool> validate = (groupQuery) =>
            {
                var allowedGroup = GetAllowedGroup(groupQuery);
                TotalNumberOfContentTypeQueriesShouldBe(allowedGroup, 2);

                ThereShouldBeAtLeastOneQueryMatchingGivenType(allowedGroup, typeof(ImageData));

                ThereShouldBeAtLeastOneQueryMatchingGivenType(allowedGroup, typeof(VideoData));

                return true;
            };

            _searchHandler.Setup(x => x.GetSearchResults(It.Is<GroupQuery>(groupQuery => validate(groupQuery)), It.IsAny<string>(), It.IsAny<Collection<string>>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();

            var query = new Query("Todd", new Dictionary<string, object>() { { "allowedTypes", new JArray("episerver.core.icontentimage", "episerver.core.icontentvideo") } });
            _searchProvider.Search(query);

            _searchHandler.Verify();
        }

        [Fact]
        public void When_allowedTypes_contains_bad_data_then_the_default_contentype_for_the_provider_should_be_added_to_the_query_expression()
        {
            Func<GroupQuery, bool> validate = (groupQuery) =>
            {
                var allowedGroup = GetAllowedGroup(groupQuery);

                Assert.Equal(1, allowedGroup.QueryExpressions.Count);

                var contentQuery = allowedGroup.QueryExpressions[0] as ContentQuery<IContent>;
                Assert.NotNull(contentQuery);

                return true;
            };

            _searchHandler.Setup(x => x.GetSearchResults(It.Is<GroupQuery>(groupQuery => validate(groupQuery)), It.IsAny<string>(), It.IsAny<Collection<string>>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();

            var query = new Query("Todd", new Dictionary<string, object>() { { "allowedTypes", "crapdata" } });
            _searchProvider.Search(query);

            _searchHandler.Verify();
        }

        [Fact]
        public void When_allowedTypes_is_not_passed_on_the_query_then_the_default_contentype_for_the_provider_should_be_added_to_the_query_expression()
        {
            Func<GroupQuery, bool> validate = (groupQuery) =>
            {
                var allowedGroup = GetAllowedGroup(groupQuery);

                Assert.Equal(1, allowedGroup.QueryExpressions.Count);

                var contentQuery = allowedGroup.QueryExpressions[0] as ContentQuery<IContent>;
                Assert.NotNull(contentQuery);

                return true;
            };

            _searchHandler.Setup(x => x.GetSearchResults(It.Is<GroupQuery>(groupQuery => validate(groupQuery)), It.IsAny<string>(), It.IsAny<Collection<string>>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();

            var query = new Query("Todd");
            _searchProvider.Search(query);

            _searchHandler.Verify();
        }

        [Fact]
        public void When_restrictedTypes_are_passed_as_params_they_should_be_added_to_the_restricted_query_expression()
        {
            _contentTypeRepository.Setup(x => x.List()).Returns(new[] { new ContentType { ModelType = typeof(ImageData) }, new ContentType { ModelType = typeof(VideoData) } });

            Func<GroupQuery, bool> validate = (groupQuery) =>
            {
                var restrictedGroup = GetRestrictedGroup(groupQuery);

                // should be only 1 query
                TotalNumberOfContentTypeQueriesShouldBe(restrictedGroup, 1);

                // query should contain VideoData
                ThereShouldBeAtLeastOneQueryMatchingGivenType(restrictedGroup, typeof(VideoData));

                return true;
            };

            _searchHandler.Setup(x => x.GetSearchResults(It.Is<GroupQuery>(groupQuery => validate(groupQuery)), It.IsAny<string>(), It.IsAny<Collection<string>>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();

            var query = new Query("track", new Dictionary<string, object> { { "allowedTypes", new JArray("episerver.core.icontentimage", "episerver.core.icontentvideo") }, { "restrictedTypes", new JArray("episerver.core.icontentvideo") } });
            _searchProvider.Search(query);

            _searchHandler.Verify();
        }

        [Fact]
        public void When_restrictedTypes_are_passed_as_params_the_contentTypeGroup_should_have_NOT_as_inner_operator()
        {
            _contentTypeRepository.Setup(x => x.List()).Returns(new[] { new ContentType { ModelType = typeof(ImageData) }, new ContentType { ModelType = typeof(VideoData) } });

            Func<GroupQuery, bool> validate = (groupQuery) =>
            {
                var contentTypeGroup = (GroupQuery)groupQuery.QueryExpressions[1];

                Assert.Equal(LuceneOperator.NOT, contentTypeGroup.InnerOperator);
                return true;
            };

            _searchHandler.Setup(x => x.GetSearchResults(It.Is<GroupQuery>(groupQuery => validate(groupQuery)), It.IsAny<string>(), It.IsAny<Collection<string>>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();

            var query = new Query("track", new Dictionary<string, object> { { "allowedTypes", new JArray("episerver.core.icontentimage", "episerver.core.icontentvideo") }, { "restrictedTypes", new JArray("episerver.core.icontentvideo") } });
            _searchProvider.Search(query);

            _searchHandler.Verify();
        }
        [Fact]
        public void When_restrictedTypes_are_not_found_in_params_the_contentTypeGroup_should_have_OR_as_inner_operator()
        {
            _contentTypeRepository.Setup(x => x.List()).Returns(new[] { new ContentType { ModelType = typeof(ImageData) }, new ContentType { ModelType = typeof(VideoData) } });

            Func<GroupQuery, bool> validate = (groupQuery) =>
            {
                var contentTypeGroup = (GroupQuery)groupQuery.QueryExpressions[1];

                Assert.Equal(LuceneOperator.OR, contentTypeGroup.InnerOperator);
                return true;
            };

            _searchHandler.Setup(x => x.GetSearchResults(It.Is<GroupQuery>(groupQuery => validate(groupQuery)), It.IsAny<string>(), It.IsAny<Collection<string>>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();

            var query = new Query("track", new Dictionary<string, object> { { "allowedTypes", new JArray("episerver.core.icontentimage", "episerver.core.icontentvideo") } });
            _searchProvider.Search(query);

            _searchHandler.Verify();
        }

        private void TotalNumberOfContentTypeQueriesShouldBe(GroupQuery query, int number) => Assert.Equal(number, query.QueryExpressions.Count);

        private void ThereShouldBeAtLeastOneQueryMatchingGivenType(GroupQuery query, Type type)
        {
            var allContentTypesQueries = query.QueryExpressions.OfType<ContentTypeQuery>();
            Assert.True(allContentTypesQueries.Any(q => q.Type == type));
        }

        private void ThereShouldNotBeAnyQueryMatchingGivenType(GroupQuery query, Type type)
        {
            var allContentTypesQueries = query.QueryExpressions.OfType<ContentTypeQuery>();
            Assert.False(allContentTypesQueries.Any(q => q.Type == type));
        }

        private GroupQuery GetAllowedGroup(GroupQuery groupQuery) => ((GroupQuery)groupQuery.QueryExpressions[1]).QueryExpressions[0] as GroupQuery;

        private GroupQuery GetRestrictedGroup(GroupQuery groupQuery) => ((GroupQuery)groupQuery.QueryExpressions[1]).QueryExpressions[1] as GroupQuery;

        public class MySpecialVideo : VideoData
        {
        }

        public class YouTubeVideo : MySpecialVideo
        {
        }
    }

    internal class FakeSearchProvider : EPiServerSearchProviderBase<IContent, ContentType>
    {
        public FakeSearchProvider(
            LocalizationService localizationService,
            ISiteDefinitionResolver siteDefinitionResolver,
            IContentTypeRepository<ContentType> contentTypeRepository,
            IContentRepository contentRepository,
            ILanguageBranchRepository languageBranchRepository,
            SearchHandler searchHandler,
            ServiceAccessor<SiteDefinition> currentSiteDefinition,
            ContentSearchHandler contentSearchHandler,
            SearchIndexConfig searchIndexConfig,
            UIDescriptorRegistry uiDescriptorRegistry,
            IContentLanguageAccessor languageResolver,
            UrlResolver urlResolver,
            TemplateResolver templateResolver)
            : base(localizationService, siteDefinitionResolver, contentTypeRepository, null, currentSiteDefinition, contentRepository, languageBranchRepository, searchHandler, contentSearchHandler, searchIndexConfig, uiDescriptorRegistry, languageResolver, urlResolver, templateResolver)
        {
        }

        public override string Area => "";

        public override string Category => "";

        protected override string IconCssClass => "";
    }
}
