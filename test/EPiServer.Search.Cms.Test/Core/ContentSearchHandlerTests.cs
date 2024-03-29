using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using EPiServer.Construction;
using EPiServer.Construction.Internal;
using EPiServer.Data.Dynamic;
using EPiServer.DataAbstraction;
using EPiServer.DataAbstraction.RuntimeModel;
using EPiServer.Models;
using EPiServer.Search;
using EPiServer.Search.Configuration;
using EPiServer.Search.Data;
using EPiServer.Search.Internal;
using EPiServer.Search.Queries;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Security;
using EPiServer.SpecializedProperties;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EPiServer.Core
{
    public class ContentSearchHandlerTests : IDisposable
    {
        private readonly ContentSearchHandler _testSubject;
        private readonly MockSearchHandler _searchHandler;
        private readonly Mock<IContentRepository> _contentRepositoryMock;
        private readonly PageReference _oldRootPage;
        private readonly Mock<IContentTypeRepository> _contentTypeRepositoryMock;
        private readonly Mock<IPrincipalAccessor> _principalAccessor;
        private readonly PageReference _oldWasteBasketPage;
        private readonly Mock<IAccessControlListQueryBuilder> _queryBuilder;
        private readonly RequestQueueHandler _requestQueueHandler;
        private readonly MockRequestHandler _requestHandler;

        public ContentSearchHandlerTests()
        {
            var searchOptions = new SearchOptions();
            searchOptions.IndexingServiceReferences.Add(new IndexingServiceReference
            {
                Name = "test service"
            });
            var options = Options.Create(searchOptions);

            _searchHandler = new MockSearchHandler();
            _contentRepositoryMock = new Mock<IContentRepository>();
            _contentTypeRepositoryMock = new Mock<IContentTypeRepository>();
            _principalAccessor = new Mock<IPrincipalAccessor>();
            _requestQueueHandler = new RequestQueueHandler(
                _requestHandler,
                null,
                options,
                null
                );
            _requestHandler = new MockRequestHandler();

            var count = 3;
            var contentList = new List<TestContent>();
            var lastContent = ContentReference.EmptyReference;
            for (var i = 0; i < count; i++)
            {
                var content = new TestContent() { ContentGuid = Guid.NewGuid(), ContentLink = new ContentReference(1000 + i), ParentLink = lastContent };
                lastContent = content.ContentLink;
                contentList.Add(content);
            }
            _queryBuilder = new Mock<IAccessControlListQueryBuilder>();

            _contentRepositoryMock.Setup(cr => cr.GetAncestors(lastContent)).Returns(contentList.Take(count - 1).Reverse<IContent>());
            _contentRepositoryMock.Setup(cr => cr.Get<IContent>(lastContent)).Returns(contentList.Last());

            _testSubject = new ContentSearchHandlerImplementation(_searchHandler,
                _contentRepositoryMock.Object,
                _contentTypeRepositoryMock.Object,
                 new SearchIndexConfig(),
                 _principalAccessor.Object,
                 _queryBuilder.Object,
                 options,
                 _requestQueueHandler,
                 _requestHandler
                 );

            _oldRootPage = ContentReference.RootPage;
            ContentReference.RootPage = new PageReference(1);

            _oldWasteBasketPage = ContentReference.WasteBasket;
            ContentReference.WasteBasket = new PageReference(2);
        }


        public void Dispose()
        {
            ContentReference.RootPage = _oldRootPage;
            ContentReference.WasteBasket = _oldWasteBasketPage;
        }

        [Fact]
        public void UpdateItem_WhenContentIsProvided_ShouldUseContentGuidAndLanguageAsId()
        {
            var sharedBlockCreator = new SharedBlockFactory(null, new ConstructorParameterResolver(),
                new ServiceLocation.ServiceAccessor<ContentDataInterceptor>(() => new ContentDataInterceptor(new ContentDataInterceptorHandler(new ConstructorParameterResolver()))), _contentTypeRepositoryMock.Object);
            _testSubject.ServiceActive = true;

            var block = sharedBlockCreator.CreateSharedBlock(typeof(BlockData));
            block.ContentGuid = Guid.NewGuid();
            (block as IChangeTrackable).Created = DateTime.Now.ToUniversalTime();
            (block as IChangeTrackable).Changed = DateTime.Now.ToUniversalTime();
            (block as ILocalizable).Language = CultureInfo.GetCultureInfo("en");

            _testSubject.UpdateItem(block);

            var expectedId = block.ContentGuid.ToString() + "|" + (block as ILocalizable).Language.Name;
            Assert.Equal(expectedId, _searchHandler.UpdatedIndexItem.Id);
        }

        [Fact]
        public void UpdateItem_WhenContentIsProvided_ShouldUseNameAsTitle()
        {
            var sharedBlockCreator = new SharedBlockFactory(null, new ConstructorParameterResolver(),
                new ServiceLocation.ServiceAccessor<ContentDataInterceptor>(() => new ContentDataInterceptor(new ContentDataInterceptorHandler(new ConstructorParameterResolver()))), _contentTypeRepositoryMock.Object);
            _testSubject.ServiceActive = true;

            var block = sharedBlockCreator.CreateSharedBlock(typeof(BlockData));
            block.ContentGuid = Guid.NewGuid();
            (block as IChangeTrackable).Created = DateTime.Now.ToUniversalTime();
            (block as IChangeTrackable).Changed = DateTime.Now.ToUniversalTime();
            (block as ILocalizable).Language = CultureInfo.GetCultureInfo("en");
            block.Name = "My Awesome Block";

            _testSubject.UpdateItem(block);

            var expectedName = block.Name;
            Assert.Equal(expectedName, _searchHandler.UpdatedIndexItem.Title);
        }

        [Fact]
        public void UpdateItem_WhenCreatedByHasValue_ShouldAddCreatedByAsAuthor()
        {
            _testSubject.ServiceActive = true;

            var page = new PageData();
            page.Property.Add(MetaDataProperties.PageCreatedBy, new PropertyString("TestUser"));
            page.Property.Add(MetaDataProperties.PageLanguageBranch, new PropertyString("en"));

            _testSubject.UpdateItem(page);

            Assert.Contains(page.CreatedBy, _searchHandler.UpdatedIndexItem.Authors);
        }

        [Fact]
        public void UpdateItem_WhenContentHasCategories_ShouldAddCategoriesToIndexItem()
        {
            _testSubject.ServiceActive = true;
            var firstCategoryId = 1;
            var page = new PageData();
            page.Property.Add(MetaDataProperties.PageLanguageBranch, new PropertyString("en"));
            page.Property.Add(MetaDataProperties.PageCategory, new PropertyCategory(new CategoryList(new int[] { firstCategoryId, 2, 3 })));

            _testSubject.UpdateItem(page);

            Assert.Equal(3, _searchHandler.UpdatedIndexItem.Categories.Count);
            Assert.Equal(firstCategoryId.ToString(), _searchHandler.UpdatedIndexItem.Categories.First());
        }

        [Fact]
        public void UpdateItem_WhenContentIsAPageAndACLGivesUserReadAccess_ShouldAddAclForThatUserToIndexItem()
        {
            _testSubject.ServiceActive = true;
            var page = new PageData();
            page.Property.Add(MetaDataProperties.PageLanguageBranch, new PropertyString("en"));
            var acl = new ContentAccessControlList();
            var userName = "user name";
            acl.Add(new AccessControlEntry(userName, AccessLevel.Read, SecurityEntityType.User));
            page.ACL = acl;

            _testSubject.UpdateItem(page);
            Assert.Equal("U:" + userName, _searchHandler.UpdatedIndexItem.AccessControlList.Single());
        }

        [Fact]
        public void UpdateItem_WhenContentIsAPageAndACLGivesRoleReadAccess_ShouldAddAclForThatRoleToIndexItem()
        {
            _testSubject.ServiceActive = true;
            var page = new PageData();
            page.Property.Add(MetaDataProperties.PageLanguageBranch, new PropertyString("en"));
            var acl = new ContentAccessControlList();
            var roleName = "role name";
            acl.Add(new AccessControlEntry(roleName, AccessLevel.Read, SecurityEntityType.Role));
            page.ACL = acl;

            _testSubject.UpdateItem(page);
            Assert.Equal("G:" + roleName, _searchHandler.UpdatedIndexItem.AccessControlList.Single());
        }

        [Fact]
        public void UpdateItem_WhenContentHasASearchableProperty_ShouldAddPropertyValueToIndexItemDisplayText()
        {
            _testSubject.ServiceActive = true;
            var propertyDefinitionName = "myProperty";
            var propertyDefinitionId = 1;
            var searchablePropertyValue = "my value";

            var page = new PageData();
            page.Property.Add(MetaDataProperties.PageLanguageBranch, new PropertyString("en"));
            page.Property.Add(MetaDataProperties.PageTypeID, new PropertyNumber(propertyDefinitionId));
            page.Property.Add(propertyDefinitionName, new PropertyStringForTest(searchablePropertyValue));

            var contentType = new PageType();
            contentType.PropertyDefinitions.Add(new PropertyDefinition { Searchable = true, Name = propertyDefinitionName });

            _contentTypeRepositoryMock.Setup(repository => repository.Load(propertyDefinitionId)).Returns(contentType);

            _testSubject.UpdateItem(page);

            Assert.Equal(searchablePropertyValue, _searchHandler.UpdatedIndexItem.DisplayText);
        }

        [Fact]
        public void UpdateItem_WhenContentHasASearchableBlockThatHasASearchableProperty_ShouldAddPropertyValueToIndexItemDisplayText()
        {
            _testSubject.ServiceActive = true;
            var blockPropertyDefinitionName = "myBlockProperty";
            var propertyDefinitionName = "myProperty";
            var pageTypeId = 1;
            var searchablePropertyValue = "my value";

            var page = new PageData();
            page.Property.Add(MetaDataProperties.PageLanguageBranch, new PropertyString("en"));
            page.Property.Add(MetaDataProperties.PageTypeID, new PropertyNumber(pageTypeId));
            var block = new BlockData();
            var propertyBlock = new PropertyBlock<BlockData>(block);
            page.Property.Add(blockPropertyDefinitionName, propertyBlock);

            var content = block as IContentData;
            content.Property.Add(propertyDefinitionName, new PropertyStringForTest(searchablePropertyValue));

            var pageType = new PageType();
            pageType.PropertyDefinitions.Add(new PropertyDefinition { Searchable = true, Name = blockPropertyDefinitionName });
            var blockType = new BlockType();
            blockType.PropertyDefinitions.Add(new PropertyDefinition { Searchable = true, Name = propertyDefinitionName });

            _contentTypeRepositoryMock.Setup(repository => repository.Load(pageTypeId)).Returns(pageType);
            _contentTypeRepositoryMock.Setup(repository => repository.Load(typeof(BlockData))).Returns(blockType);

            _testSubject.UpdateItem(page);

            Assert.Equal(searchablePropertyValue, _searchHandler.UpdatedIndexItem.DisplayText);
        }

        [Fact]
        public void UpdateItem_WhenContentHasUnassignedCategoryProperty_ShouldClearPropertyFromIndex()
        {
            _testSubject.ServiceActive = true;
            var propertyDefinitionName = "myCategory";
            var pageType = new PageType() { ID = 1 };

            var page = new PageData();
            page.Property.Add(propertyDefinitionName, new PropertyCategory());
            page.Property.Add(MetaDataProperties.PageTypeID, new PropertyNumber(pageType.ID));

            pageType.PropertyDefinitions.Add(new PropertyDefinition { Searchable = true, Name = propertyDefinitionName });

            _contentTypeRepositoryMock.Setup(repository => repository.Load(pageType.ID)).Returns(pageType);

            _testSubject.UpdateItem(page);

            Assert.Equal(string.Empty, _searchHandler.UpdatedIndexItem.DisplayText);
        }

        [Fact]
        public void GetItemType_WhenTypeDoesNotInheritOtherType_ShouldBeTypeAndBaseItemType()
        {
            var result = _testSubject.GetItemType(typeof(TestContent));

            var expected = string.Concat(
                ContentSearchHandler.GetItemTypeSection<TestContent>(),
                ContentSearchHandlerImplementation.ItemTypeSeparator,
                ContentSearchHandlerImplementation.BaseItemType);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetItemType_WhenTypeInheritDirectlyFromContentData_ResultShouldContainTypeNameContentDataAndBaseItemType()
        {
            var result = _testSubject.GetItemType(typeof(FirstLevelInheritedContent));

            var expected = string.Concat(
                ContentSearchHandler.GetItemTypeSection<FirstLevelInheritedContent>(),
                ContentSearchHandlerImplementation.ItemTypeSeparator,
                ContentSearchHandler.GetItemTypeSection<ContentData>(),
                ContentSearchHandlerImplementation.ItemTypeSeparator,
                ContentSearchHandlerImplementation.BaseItemType);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetItemType_WhenTypeInheritFromContentDataInTwoLevels_ResultShouldContainTypeNameAndParentAndContentDataAndBaseItemType()
        {
            var result = _testSubject.GetItemType(typeof(SecondLevelInheritedContent));

            var expected = string.Concat(
                ContentSearchHandler.GetItemTypeSection<SecondLevelInheritedContent>(),
                ContentSearchHandlerImplementation.ItemTypeSeparator,
                ContentSearchHandler.GetItemTypeSection<FirstLevelInheritedContent>(),
                ContentSearchHandlerImplementation.ItemTypeSeparator,
                ContentSearchHandler.GetItemTypeSection<ContentData>(),
                ContentSearchHandlerImplementation.ItemTypeSeparator,
                ContentSearchHandlerImplementation.BaseItemType);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetVirtualPathNodes_ShouldReturnListOfAllAncestors()
        {
            var result = _testSubject.GetVirtualPathNodes(new ContentReference(1002)).ToList();

            Assert.Equal(3, result.Count());

            for (var i = 0; i < result.Count(); i++)
            {
                Assert.True(Guid.TryParse(result[i], out var guid));
            }
        }

        [Fact]
        public void GetContent_WhenIndexItemIsNull_ShouldReturnNull()
        {
            IndexResponseItem indexItem = null;
            var result = _testSubject.GetContent<IContent>(indexItem);
            Assert.Null(result);
        }

        [Fact]
        public void GetContent_WhenIdIsNotAGuid_ShouldReturnNull()
        {
            var indexItem = new IndexResponseItem("not a guid");
            var result = _testSubject.GetContent<IContent>(indexItem);
            Assert.Null(result);
        }

        [Fact]
        public void GetContent_WhenIdIsAGuidWithLanguage_ShouldGetContentFromRepository()
        {
            var guid = Guid.NewGuid();
            var id = string.Format("{0}{1}{2}", guid, ContentSearchHandlerImplementation.SearchItemIdSeparator, "en");
            var indexItem = new IndexResponseItem(id)
            {
                Culture = "en"
            };

            var result = _testSubject.GetContent<IContent>(indexItem);
            _contentRepositoryMock.Verify(repository => repository.Get<IContent>(guid, It.IsAny<LoaderOptions>()), Times.Once());
        }

        [Fact]
        public void GetContent_WhenCultureIsNull_ShouldUseLanguageSelectorFactoryToGetLanguageSelector()
        {
            var guid = Guid.NewGuid();
            var id = string.Format("{0}{1}{2}", guid, ContentSearchHandlerImplementation.SearchItemIdSeparator, "en");
            var indexItem = new IndexResponseItem(id)
            {
                Culture = null
            };

            var culture = CultureInfo.GetCultureInfo("fi");
            _contentRepositoryMock.Setup(c => c.Get<IContent>(It.IsAny<Guid>(), It.IsAny<LoaderOptions>()))
                .Callback<Guid, LoaderOptions>((c, l) =>
                    {
                        culture = l.Get<LanguageLoaderOption>().Language;
                    });

            var result = _testSubject.GetContent<IContent>(indexItem);

            Assert.Null(culture);
        }

        [Fact]
        public void GetContent_WhenFilterByCultureIsTrue_ShouldGetLanguageSelectorUsingAutoDetect()
        {
            var guid = Guid.NewGuid();
            var id = string.Format("{0}{1}{2}", guid, ContentSearchHandlerImplementation.SearchItemIdSeparator, "en");
            var indexItem = new IndexResponseItem(id);

            LoaderOptions loaderOptions = null;
            _contentRepositoryMock.Setup(c => c.Get<IContent>(It.IsAny<Guid>(), It.IsAny<LoaderOptions>()))
                .Callback<Guid, LoaderOptions>((c, l) =>
                {
                    loaderOptions = l;
                });

            _testSubject.GetContent<IContent>(indexItem, true);
            Assert.Null(loaderOptions.Get<LanguageLoaderOption>().Language);
            Assert.Equal(LanguageBehaviour.Fallback, loaderOptions.Get<LanguageLoaderOption>().FallbackBehaviour);
        }

        [Fact]
        public void GetSearchResult_WhenConfigIsInactive_ShouldReturnNull()
        {
            _testSubject.ServiceActive = false;

            var result = _testSubject.GetSearchResults<IContent>(string.Empty, 1, 100);

            Assert.Null(result);
        }

        [Fact]
        public void GetSearchResult_WhenQueryIsExecuted_ShouldUseSuppliedExpression()
        {
            _contentRepositoryMock.Setup(r => r.Get<IContent>(It.IsAny<ContentReference>()))
                .Returns(new TestContent() { ContentLink = new ContentReference(1) });

            _testSubject.ServiceActive = true;

            var searchString = "A custom search string";
            var result = _testSubject.GetSearchResults<IContent>(searchString, 1, 100);

            var executedQuery = _searchHandler.ExecutedSearchQuery as GroupQuery;
            Assert.Equal(searchString, executedQuery.QueryExpressions.OfType<FieldQuery>().First().Expression);
        }

        [Fact]
        public void GetSearchResult_WhenQueryIsExecuted_ContentQueryShouldBeAdded()
        {
            var sharedBlockCreator = new SharedBlockFactory(null, new ConstructorParameterResolver(),
                new ServiceLocation.ServiceAccessor<ContentDataInterceptor>(() => new ContentDataInterceptor(new ContentDataInterceptorHandler(new ConstructorParameterResolver()))), _contentTypeRepositoryMock.Object);
            var content = sharedBlockCreator.CreateSharedBlock(typeof(BlockData));
            content.ContentLink = new ContentReference(1);
            _contentRepositoryMock.Setup(r => r.Get<IContent>(It.IsAny<ContentReference>())).Returns(content);

            _testSubject.ServiceActive = true;

            var searchString = "A custom search string";
            var result = _testSubject.GetSearchResults<IContent>(searchString, 1, 100);

            var executedQuery = _searchHandler.ExecutedSearchQuery as GroupQuery;

            Assert.True(executedQuery.QueryExpressions.OfType<ContentQuery<IContent>>().Any());
        }

        [Fact]
        public void GetSearchResult_WhenQueryIsExecuted_CurrentUserShouldBeAddedToAcl()
        {
            var userName = "testUser";

            var mockPrincipal = new Mock<IPrincipal>();
            mockPrincipal.Setup(p => p.Identity.Name).Returns(userName);

            _principalAccessor.Setup(p => p.Principal).Returns(mockPrincipal.Object);

            _queryBuilder.Setup(q => q.AddUser(It.IsAny<AccessControlListQuery>(), mockPrincipal.Object, It.IsAny<object>()))
                .Callback<AccessControlListQuery, IPrincipal, object>((q, p, o) => { q.AddUser(p.Identity.Name); });

            var sharedBlockCreator = new SharedBlockFactory(null, new ConstructorParameterResolver(),
                new ServiceLocation.ServiceAccessor<ContentDataInterceptor>(() => new ContentDataInterceptor(new ContentDataInterceptorHandler(new ConstructorParameterResolver()))), _contentTypeRepositoryMock.Object);
            var content = sharedBlockCreator.CreateSharedBlock(typeof(BlockData));
            content.ContentLink = new ContentReference(1);
            _contentRepositoryMock.Setup(r => r.Get<IContent>(It.IsAny<ContentReference>())).Returns(content);

            _testSubject.ServiceActive = true;

            var result = _testSubject.GetSearchResults<IContent>(string.Empty, 1, 100);

            var executedQuery = _searchHandler.ExecutedSearchQuery as GroupQuery;
            Assert.Contains("U:" + userName, ((AccessControlListQuery)executedQuery.QueryExpressions[2]).Items);

        }

        [Fact]
        public void RemoveItemsByVirtualPath_WhenNodesIsNull_ShouldThrowArgumentNullException() => Assert.Throws<ArgumentNullException>(() => _testSubject.RemoveItemsByVirtualPath(null));

        [Fact]
        public void RemoveItemsByVirtualPath_WhenNodesIsEmpty_ShouldNotSendAnyItemToSearchHandler()
        {
            var collection = new List<string>();
            _testSubject.ServiceActive = true;
            _testSubject.RemoveItemsByVirtualPath(collection);
            Assert.Null(_searchHandler.UpdatedIndexItem);
        }

        [Fact]
        public void RemoveItemsByVirtualPath_WhenNodesHaveValues_ShouldHaveAutoUpdateVirtualPathSetToTrue()
        {
            var node1 = "node1";
            var collection = new List<string> { node1 };
            _testSubject.ServiceActive = true;
            _testSubject.RemoveItemsByVirtualPath(collection);
            var result = _searchHandler.UpdatedIndexItem as IndexRequestItem;
            Assert.True(result.AutoUpdateVirtualPath.Value);
        }

        [Fact]
        public void RemoveItemsByVirtualPath_WhenNodesHaveValues_ShouldAddValuesToIndexRequestItem()
        {
            var node1 = "node1";
            var node2 = "node2";
            var collection = new List<string> { node1, node2 };
            _testSubject.ServiceActive = true;
            _testSubject.RemoveItemsByVirtualPath(collection);
            Assert.Equal(2, _searchHandler.UpdatedIndexItem.VirtualPathNodes.Count);
            Assert.Equal(node1, _searchHandler.UpdatedIndexItem.VirtualPathNodes[0]);
            Assert.Equal(node2, _searchHandler.UpdatedIndexItem.VirtualPathNodes[1]);
        }

        [Fact]
        public void MoveItem_WithNullContentLink_ShouldNotSendAnyItemToSearchHandler()
        {
            _testSubject.ServiceActive = true;
            _testSubject.MoveItem(null);
            Assert.Null(_searchHandler.UpdatedIndexItem);
        }

        [Fact]
        public void MoveItem_WithContentLinkForAPage_ShouldAddContentFromPageToIndexItem()
        {
            _testSubject.ServiceActive = true;

            var contentReference = new ContentReference(123);
            var createdBy = "Test user";
            IContent page = new PageData();
            page.Property.Add(MetaDataProperties.PageCreatedBy, new PropertyString(createdBy));
            page.Property.Add(MetaDataProperties.PageLanguageBranch, new PropertyString("en"));

            _contentRepositoryMock.Setup(repository => repository.TryGet<IContent>(contentReference, It.IsAny<CultureInfo>(), out page)).Returns(true);

            _testSubject.MoveItem(contentReference);

            Assert.Equal(createdBy, _searchHandler.UpdatedIndexItem.Authors.Single());
        }

        [Fact]
        public void IndexPublishedContent_ShouldIndexAllContentInRepository()
        {
            _testSubject.ServiceActive = true;

            var childPage = new PageData();
            var contentReference = new ContentReference(123);
            var createdBy = "Test user";
            childPage.Property.Add(MetaDataProperties.PageCreatedBy, new PropertyString(createdBy));
            childPage.Property.Add(MetaDataProperties.PageLanguageBranch, new PropertyString("en"));
            childPage.Property.Add(MetaDataProperties.PageWorkStatus, new PropertyVersionStatus() { Value = VersionStatus.Published });
            childPage.Property.Add(MetaDataProperties.PageLink, new PropertyContentReference(contentReference));

            var childPages = new PageData[] { childPage };
            _contentRepositoryMock.Setup(repository => repository.GetLanguageBranches<IContent>(ContentReference.RootPage)).Returns(new PageData[] { new PageData() });
            _contentRepositoryMock.Setup(repository => repository.GetLanguageBranches<IContent>(contentReference)).Returns(childPages);
            _contentRepositoryMock.Setup(repository => repository.GetChildren<IContent>(ContentReference.RootPage, CultureInfo.InvariantCulture)).Returns(childPages);
            _contentRepositoryMock.Setup(repository => repository.Get<IContent>(contentReference)).Returns(childPage);

            var ddsMock = new Mock<DynamicDataStore>(null);
            var ddsFactoryMock = new Mock<DynamicDataStoreFactory>();
            ddsFactoryMock.Setup(x => x.GetStore(typeof(ContentIndexingResult))).Returns(ddsMock.Object);
            DynamicDataStoreFactory.Instance = ddsFactoryMock.Object;

            _testSubject.IndexPublishedContent();

            Assert.Equal(createdBy, _requestHandler.UpdatedIndexItem.Items.ToList()[0].Authors.Single());
        }

        private class TestContent : IContent
        {
            public string Name { get; set; }
            public ContentReference ContentLink { get; set; }
            public ContentReference ParentLink { get; set; }
            public Guid ContentGuid { get; set; }
            public int ContentTypeID { get; set; }
            public PropertyDataCollection Property { get; set; }
            public bool IsNull { get; set; }
            public bool IsDeleted { get; set; }

        }

        private class MockSearchHandler : SearchHandler
        {
            public MockSearchHandler()
                : base(null, null, Options.Create<SearchOptions>(new SearchOptions())) { }

            public IndexItemBase UpdatedIndexItem { get; set; }
            public IQueryExpression ExecutedSearchQuery { get; set; }

            public override void UpdateIndex(IndexRequestItem item) => UpdatedIndexItem = item;

            public override void UpdateIndex(IndexRequestItem item, string namedIndexingService) => UpdatedIndexItem = item;

            public override SearchResults GetSearchResults(IQueryExpression queryExpression, int page, int pageSize)
            {
                ExecutedSearchQuery = queryExpression;

                return null;
            }

            public override SearchResults GetSearchResults(IQueryExpression queryExpression, string namedIndexingService, System.Collections.ObjectModel.Collection<string> namedIndexes, int page, int pageSize)
            {
                ExecutedSearchQuery = queryExpression;

                return null;
            }

        }

        private class MockRequestHandler : RequestHandler
        {
            public MockRequestHandler() : base(Options.Create(new SearchOptions()))
            {
            }

            public FeedModel UpdatedIndexItem { get; set; }

            public override bool SendRequest(FeedModel feed, string namedIndexingService)
            {
                UpdatedIndexItem = feed;
                return true;
            }
        }

        private class FirstLevelInheritedContent : ContentData { }
        private class SecondLevelInheritedContent : FirstLevelInheritedContent { }

        private class PropertyStringForTest : PropertyString
        {
            public PropertyStringForTest(string value) : base(value) { }

            public override string ToWebString() => Value.ToString();
        }
    }

}
