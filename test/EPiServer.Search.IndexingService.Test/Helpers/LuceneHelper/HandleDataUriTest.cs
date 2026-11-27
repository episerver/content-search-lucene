using System;
using System.Collections.ObjectModel;
using System.IO;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(IndexingService.Helpers.LuceneHelper), nameof(IndexingService.Helpers.LuceneHelper.HandleDataUri))]
    public class HandleDataUriTest : LuceneHelperTestBase, IDisposable
    {
        // Use a unique temp folder per test so the tests work on any OS/agent (no hard-coded c:\ paths).
        private readonly string _testRoot = Path.Combine(Path.GetTempPath(), nameof(HandleDataUriTest), Guid.NewGuid().ToString("N"));
        private Lucene.Net.Store.FSDirectory _indexDirectory;

        public HandleDataUriTest()
        {
            Directory.CreateDirectory(_testRoot);
        }

        public void Dispose()
        {
            _indexDirectory?.Dispose();
            try
            {
                Directory.Delete(_testRoot, true);
            }
            catch (IOException)
            {
                // Best effort cleanup; the folder lives under the temp path.
            }
        }

        [Fact]
        public void HandleDataUri_WhenUriStringIsNotUri_ShouldReturnFalse()
        {
            var feed = new FeedItemModel();

            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns((string)null);

            var classInstant = SetupMock();
            var result = classInstant.HandleDataUri(feed, new NamedIndex("testindex1"));
            Assert.False(result);
        }

        [Fact]
        public void HandleDataUri_WhenFileNotExist_ShouldReturnFalse()
        {
            var feed = new FeedItemModel();

            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns(ToFileUri(Path.Combine(_testRoot, "Index", "file.txt")));

            var classInstant = SetupMock();
            var result = classInstant.HandleDataUri(feed, new NamedIndex("testindex1"));
            Assert.False(result);
        }

        [Fact]
        public void HandleDataUri_WhenUriIsFileAndRunAdd_ShouldReturnTrue()
        {
            var dataFilePath = CreateFileForTest();

            var feed = new FeedItemModel()
            {
                Id = "Id",
                Title = "Header test",
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


            _commonFuncMock.Setup(x => x.GetFileUriContent(It.IsAny<Uri>())).Returns("sometext");

            _indexDirectory = Lucene.Net.Store.FSDirectory.Open(new DirectoryInfo(Path.Combine(_testRoot, Guid.NewGuid().ToString("N"), "Main")));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => _indexDirectory);

            _feedHelperMock.Setup(x => x.PrepareAuthors(It.IsAny<FeedItemModel>())).Returns("someone");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.IsAny<string>())).Returns("something");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns(dataFilePath);
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameIndexAction))).Returns("add");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameReferenceId))).Returns("testindex1_ref");


            var classInstant = SetupMock();
            var result = classInstant.HandleDataUri(feed, namedIndexMock.Object);
            Assert.True(result);
        }

        [Fact]
        public void HandleDataUri_WhenUriIsFileAndRunUpdate_ShouldReturnTrue()
        {
            var dataFilePath = CreateFileForTest();

            var feed = new FeedItemModel()
            {
                Id = "Id",
                Title = "Header test",
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


            _commonFuncMock.Setup(x => x.GetFileUriContent(It.IsAny<Uri>())).Returns("sometext");

            _indexDirectory = Lucene.Net.Store.FSDirectory.Open(new DirectoryInfo(Path.Combine(_testRoot, Guid.NewGuid().ToString("N"), "Main")));

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            namedIndexMock.SetupGet(x => x.Directory).Returns(() => _indexDirectory);

            _feedHelperMock.Setup(x => x.PrepareAuthors(It.IsAny<FeedItemModel>())).Returns("someone");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.IsAny<string>())).Returns("something");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameDataUri))).Returns(dataFilePath);
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameIndexAction))).Returns("update");
            _feedHelperMock.Setup(x => x.GetAttributeValue(It.IsAny<FeedItemModel>(), It.Is<string>(s => s == IndexingServiceSettings.SyndicationItemAttributeNameReferenceId))).Returns("testindex1_ref");


            var classInstant = SetupMock();
            var result = classInstant.HandleDataUri(feed, namedIndexMock.Object);
            Assert.True(result);
        }

        private string CreateFileForTest()
        {
            var path = Path.Combine(_testRoot, "test.txt");
            File.WriteAllLines(path, new[] { "Hello", "And", "Welcome" });
            return ToFileUri(path);
        }

        // HandleDataUri parses the value with UriKind.RelativeOrAbsolute. On Unix a rooted path such as "/tmp/x"
        // is then treated as a relative URI (and Uri.IsFile throws), so pass an absolute file:// URI instead.
        private static string ToFileUri(string path) => new Uri(path, UriKind.Absolute).AbsoluteUri;
    }
}
