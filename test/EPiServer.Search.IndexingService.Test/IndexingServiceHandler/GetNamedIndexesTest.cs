using System;
using System.Linq;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.IndexingServiceHandler
{
    [Trait(nameof(EPiServer.Search.IndexingService.IndexingServiceHandler), nameof(EPiServer.Search.IndexingService.IndexingServiceHandler.GetNamedIndexes))]
    public class GetNamedIndexesTest : IndexingServiceHandlerTestBase
    {
        [Fact]
        public void GetNamedIndexes_ShouldReturnAllNamedIndexes()
        {
            var namedIndexMock1 = new Mock<NamedIndex>(Guid.NewGuid().ToString());
            var namedIndexMock2 = new Mock<NamedIndex>(Guid.NewGuid().ToString());
            IndexingServiceSettings.NamedIndexElements.Add(namedIndexMock1.Object.Name, new Configuration.NamedIndexElement() { Name = namedIndexMock1.Object.Name });
            IndexingServiceSettings.NamedIndexElements.Add(namedIndexMock2.Object.Name, new Configuration.NamedIndexElement() { Name = namedIndexMock2.Object.Name });

            var classInstant = SetupMock();
            var result = classInstant.GetNamedIndexes();

            Assert.True(result.Items.Count() > 0);
        }
    }
}
