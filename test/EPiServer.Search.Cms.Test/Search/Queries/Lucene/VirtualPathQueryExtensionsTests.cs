using EPiServer.Core;
using EPiServer.Search.Queries.Lucene;
using Moq;
using Xunit;

namespace EPiServer.UnitTests.Search.Queries.Lucene
{
    public class VirtualPathQueryExtensionsTests
    {
        [Fact]
        public void AddContentNodes_WhenContentLinkIsNull_ShouldAddNothing()
        {
            var query = new VirtualPathQuery();

            query.AddContentNodes(null, Mock.Of<ContentSearchHandler>());

            Assert.Empty(query.VirtualPathNodes);
        }

        [Fact]
        public void AddContentNodes_WhenContentLinkHasValue_ShouldAddNodes()
        {
            var query = new VirtualPathQuery();
            var mock = new Mock<ContentSearchHandler>();

            mock.Setup(m => m.GetVirtualPathNodes(It.IsAny<ContentReference>())).Returns(new[] { "1", "2" });

            query.AddContentNodes(new ContentReference(1), mock.Object);

            Assert.Equal(2, query.VirtualPathNodes.Count);
        }

    }
}
