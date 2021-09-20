using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using EPiServer.Cms.Shell.Search;
using EPiServer.Cms.Shell.Search.Internal;
using EPiServer.Cms.Shell.UI.Test.Fakes;
using EPiServer.Construction;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAbstraction.RuntimeModel;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Search;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Security;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EPiServer.Cms.Shell.UI.Test.Search
{
    public class when_two_different_blocks_where_found_and_they_exists_in_preferred_culture : BlockSearchProviderFilterTestContext
    {
        protected override SearchResults GetSearchResults()
        {
            var searchResults = new SearchResults();

            searchResults.IndexResponseItems.Add(new IndexResponseItem(Block1Guid.ToString()));
            searchResults.IndexResponseItems.Add(new IndexResponseItem(Block2Guid.ToString()));

            return searchResults;
        }

        public override void Initialize()
        {
            base.Initialize();

            AddBlock(Block1Guid, Block1ContentRef, CultureInfo.CreateSpecificCulture("en"));
            AddBlock(Block2Guid, Block2ContentRef, CultureInfo.CreateSpecificCulture("en"));

            _languageResolver.Setup(x => x.Language).Returns(CultureInfo.CreateSpecificCulture("en"));

            _results = _searchProvider.Search(new Query("yo"));
        }

        [Fact]
        public void the_content_found_should_be_added()
        {
            _contentRepository.Verify(r => r.Get<IContent>(It.IsAny<ContentReference>()), Times.Never());
        }
    }

    public class when_the_same_block_was_found_in_different_languages_and_none_match_the_preferredCulture : BlockSearchProviderFilterTestContext
    {
        protected override SearchResults GetSearchResults()
        {
            var searchResults = new SearchResults();

            searchResults.IndexResponseItems.Add(new IndexResponseItem(Block1Guid.ToString()));
            searchResults.IndexResponseItems.Add(new IndexResponseItem(Block2Guid.ToString()));

            return searchResults;
        }

        public override void Initialize()
        {
            base.Initialize();

            AddBlock(Block1Guid, Block1ContentRef, CultureInfo.CreateSpecificCulture("en-US"));
            AddBlock(Block2Guid, Block1ContentRef, CultureInfo.CreateSpecificCulture("sv-SE"), true);

            _results = _searchProvider.Search(new Query("yo"));
        }

        [Fact]
        public void the_master_language_version_should_be_returned()
        {
            Assert.Equal("sv-SE", _results.FirstOrDefault().Metadata["LanguageBranch"]);
        }
    }

    public class when_the_same_block_was_found_in_different_languages_and_one_match_the_preferredCulture : BlockSearchProviderFilterTestContext
    {
        protected override SearchResults GetSearchResults()
        {
            var searchResults = new SearchResults();

            searchResults.IndexResponseItems.Add(new IndexResponseItem(Block1Guid.ToString()));
            searchResults.IndexResponseItems.Add(new IndexResponseItem(Block2Guid.ToString()));

            return searchResults;
        }

        public override void Initialize()
        {
            base.Initialize();

            _languageResolver.Setup(x => x.Language).Returns(CultureInfo.CreateSpecificCulture("en-US"));

            AddBlock(Block1Guid, Block1ContentRef, CultureInfo.CreateSpecificCulture("en-US"));
            AddBlock(Block2Guid, Block1ContentRef, CultureInfo.CreateSpecificCulture("sv-SE"), true);

            _results = _searchProvider.Search(new Query("yo"));
        }

        [Fact]
        public void the_version_matching_the_preferred_culture_should_be_returned()
        {
            Assert.Equal(1, _results.Count());
            Assert.Equal("en-US", _results.FirstOrDefault().Metadata["LanguageBranch"]);
        }
    }

    public class when_the_same_block_was_found_in_different_languages_and_does_not_match_the_preferredCulture_or_is_a_master_language_version : BlockSearchProviderFilterTestContext
    {
        protected override SearchResults GetSearchResults()
        {
            var searchResults = new SearchResults();

            searchResults.IndexResponseItems.Add(new IndexResponseItem(Block1Guid.ToString()));
            searchResults.IndexResponseItems.Add(new IndexResponseItem(Block2Guid.ToString()));

            return searchResults;
        }

        public override void Initialize()
        {
            base.Initialize();

            AddBlock(Block1Guid, Block1ContentRef, CultureInfo.CreateSpecificCulture("en-US"));
            AddBlock(Block2Guid, Block1ContentRef, CultureInfo.CreateSpecificCulture("sv-SE"));

            _results = _searchProvider.Search(new Query("yo"));
        }

        [Fact]
        public void the_first_duplicate_item_should_be_returned()
        {
            Assert.Equal("en-US", _results.FirstOrDefault().Metadata["LanguageBranch"]);
        }
    }

    public abstract class BlockSearchProviderFilterTestContext
    {
        protected Mock<SearchHandler> _searchHandler;
        protected Mock<IContentRepository> _contentRepository;
        protected BlockSearchProvider _searchProvider;
        protected Mock<ILanguageBranchRepository> _languageBranchRespository;
        protected Mock<ContentSearchHandler> _contentSearchHandler;
        protected Mock<IContentTypeRepository<BlockType>> _contentTypeRepository;
        protected Mock<ISiteDefinitionResolver> _siteDefinitionResolver;
        protected Mock<IContentLanguageAccessor> _languageResolver;

        protected Guid Block1Guid = new Guid("{979E6C95-94DE-4DE7-A08D-DFBAF15F18D7}");
        protected ContentReference Block1ContentRef = new ContentReference(1);

        protected Guid Block2Guid = new Guid("{EE9BC746-6396-461D-B693-829394D170EE}");
        protected ContentReference Block2ContentRef = new ContentReference(2);

        protected IEnumerable<SearchResult> _results;

        protected abstract SearchResults GetSearchResults();

        public BlockSearchProviderFilterTestContext()
        {
            this.Initialize();
        }

        public virtual void Initialize()
        {
            _searchHandler = new Mock<SearchHandler>(null, null, Options.Create(new SearchOptions()));
            _contentTypeRepository = new Mock<IContentTypeRepository<BlockType>>();

            _contentRepository = new Mock<IContentRepository>();
            _contentSearchHandler = new Mock<ContentSearchHandler>();

            _languageBranchRespository = new Mock<ILanguageBranchRepository>();
            _languageBranchRespository.Setup(r => r.ListEnabled()).Returns(new List<LanguageBranch>());

            _siteDefinitionResolver = new Mock<ISiteDefinitionResolver>();
            _siteDefinitionResolver.Setup(s => s.GetByContent(It.IsAny<ContentReference>(), It.IsAny<bool>())).Returns(new SiteDefinition());

            _searchHandler.Setup(s => s.GetSearchResults(It.IsAny<GroupQuery>(), It.IsAny<string>(), It.IsAny<Collection<string>>(), It.IsAny<int>(), It.IsAny<int>())).Returns(GetSearchResults);

            _languageResolver = new Mock<IContentLanguageAccessor>();

            var uiDescriptorRegistry = new Mock<UIDescriptorRegistry>(null, null);

            _searchProvider = new BlockSearchProvider(
                LocalizationService.Current,
                _siteDefinitionResolver.Object,
                _contentTypeRepository.Object,
                null,
                () => new FakeSiteDefinition(),
                _contentRepository.Object,
                _languageBranchRespository.Object,
                _searchHandler.Object,
                _contentSearchHandler.Object,
                new SearchIndexConfig(),
                uiDescriptorRegistry.Object,
                _languageResolver.Object,
                Mock.Of<UrlResolver>(),
                Mock.Of<TemplateResolver>(),
                Mock.Of<IBlobResolver>());

            _searchProvider.EditPath = (cr, a, b) => { return string.Empty; };
            _searchProvider.HasAdminAccess = () => true;
            _searchProvider.IsSearchActive = true;
        }

        protected void AddBlock(Guid id, ContentReference contentReference, CultureInfo culture, bool isMasterLanguageVersion = false)
        {
            var block = new Mock<BlockData>();
            block.CallBase = true;

            var content = block.As<IContent>();

            content.SetupGet(x => x.ContentGuid).Returns(id);
            content.SetupGet(x => x.ContentLink).Returns(contentReference);
            content.SetupGet(x => x.ParentLink).Returns(ContentReference.EmptyReference);

            var versionable = content.As<IVersionable>();
            versionable.SetupGet(x => x.Status).Returns(VersionStatus.Published);

            var securable = content.As<ISecurable>();
            securable.Setup(x => x.GetSecurityDescriptor())
                .Returns(new ContentAccessControlList
                {
                    new AccessControlEntry(PrincipalInfo.CurrentPrincipal.Identity.Name, AccessLevel.Administer | AccessLevel.Read, SecurityEntityType.User)
                });

            var localizable = content.As<ILocalizable>();
            localizable.SetupGet(x => x.Language).Returns(culture);
            localizable.SetupGet(x => x.ExistingLanguages).Returns(new[] { culture });
            if (isMasterLanguageVersion)
            {
                localizable.SetupGet(x => x.MasterLanguage).Returns(culture);
            }

            _contentRepository.Setup(r => r.Get<IContent>(It.Is<Guid>(guid => guid.Equals(id)), It.IsAny<LoaderOptions>()))
                .Returns(content.Object);

            _contentRepository.Setup(r => r.Get<IContent>(It.Is<ContentReference>(cr => cr.Equals(contentReference)))).Returns(content.Object).Verifiable();

            _contentSearchHandler.Setup(h => h.GetContent<IContent>(It.Is<IndexItemBase>(i => Guid.Parse(i.Id).Equals(id)), It.IsAny<Boolean>())).Returns(content.Object);
        }
    }
}
