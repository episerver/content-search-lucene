using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.IndexingServiceHandler
{
    [Trait(nameof(EPiServer.Search.IndexingService.IndexingServiceHandler), nameof(EPiServer.Search.IndexingService.IndexingServiceHandler.ResetNamedIndex))]
    public class ResetNamedIndexTest : IndexingServiceHandlerTestBase
    {
        [Fact]
        public void ResetNamedIndex_WhenNamedIndexNotExist_ShouldThrowExeption()
        {
            var namedIndexMock1 = new Mock<NamedIndex>(Guid.NewGuid().ToString());
            var namedIndexMock2 = new Mock<NamedIndex>(Guid.NewGuid().ToString());
            IndexingServiceSettings.NamedIndexElements.Add(namedIndexMock1.Object.Name, new Configuration.NamedIndexElement() { Name = namedIndexMock1.Object.Name });

            _responseExceptionHelperMock.Setup(x => x.HandleServiceError(It.IsAny<string>())).Throws(new HttpResponseException() { Status = 500 });

            var classInstant = SetupMock();
            var caughtExeption = Assert.Throws<HttpResponseException>(() => classInstant.ResetNamedIndex(namedIndexMock2.Object.Name));

            Assert.Equal(500, caughtExeption.Status);
        }

        [Fact]
        public void ResetNamedIndex_WhenNamedIndexExist_ShouldWork()
        {
            var namedIndexMock1 = new Mock<NamedIndex>(Guid.NewGuid().ToString());
            IndexingServiceSettings.NamedIndexElements.Add(namedIndexMock1.Object.Name, new Configuration.NamedIndexElement() { Name = namedIndexMock1.Object.Name });

            var classInstant = SetupMock();
            classInstant.ResetNamedIndex(namedIndexMock1.Object.Name);
        }
    }
}
