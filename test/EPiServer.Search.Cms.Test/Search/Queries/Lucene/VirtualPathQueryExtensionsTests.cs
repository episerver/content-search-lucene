using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Core;
using Moq;
using EPiServer.DataAbstraction.RuntimeModel;
using EPiServer.Construction;
using Xunit;

namespace EPiServer.UnitTests.Search.Queries.Lucene
{
    public class VirtualPathQueryExtensionsTests
    {
        [Fact]
        public void AddContentNodes_WhenContentLinkIsNull_ShouldAddNothing()
        {
            VirtualPathQuery query = new VirtualPathQuery();

            query.AddContentNodes(null, Mock.Of<ContentSearchHandler>());

            Assert.Equal(0, query.VirtualPathNodes.Count);
        }

        [Fact]
        public void AddContentNodes_WhenContentLinkHasValue_ShouldAddNodes()
        {
            VirtualPathQuery query = new VirtualPathQuery();
            var mock = new Mock<ContentSearchHandler>();

            mock.Setup(m => m.GetVirtualPathNodes(It.IsAny<ContentReference>())).Returns(new[] { "1", "2" });

            query.AddContentNodes(new ContentReference(1), mock.Object);

            Assert.Equal(2, query.VirtualPathNodes.Count);
        }

    }
}
