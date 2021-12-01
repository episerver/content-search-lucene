using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Controllers
{
    [Trait(nameof(EPiServer.Search.IndexingService.Controllers.IndexingController), nameof(EPiServer.Search.IndexingService.Controllers.IndexingController.ResetIndex))]
    public class ResetIndexTest : IndexingControllerTestBase
    {
        [Fact]
        public void ResetIndex_WhenAccessKeyIsInvalid_ShouldReturnUnauthorizedStatusCode()
        {
            var namedIndex = "";
            var accessKey = "";

            _securityHandlerMock.Setup(x => x.IsAuthenticated(It.IsAny<string>(), It.IsAny<Security.AccessLevel>())).Returns(false);
            _responseExceptionHelperMock.Setup(x => x.HandleServiceUnauthorized(It.IsAny<string>())).Throws(new HttpResponseException() { Status = 401 });

            var classInstant = SetupMock();
            var caughtException = Assert.Throws<HttpResponseException>(() => classInstant.ResetIndex(namedIndex, accessKey));
            Assert.Equal(401, caughtException.Status);
        }

        [Fact]
        public void ResetIndex_WhenNamedindexIsNotExist_ShouldReturnError500()
        {
            var namedIndex = "";
            var accessKey = "local";

            _securityHandlerMock.Setup(x => x.IsAuthenticated(It.IsAny<string>(), It.IsAny<Security.AccessLevel>())).Returns(true);
            _indexingServiceHandlerMock.Setup(x => x.ResetNamedIndex(It.IsAny<string>())).Throws(new HttpResponseException() { Status = 500 });

            var classInstant = SetupMock();
            var caughtException = Assert.Throws<HttpResponseException>(() => classInstant.ResetIndex(namedIndex, accessKey));
            Assert.Equal(500, caughtException.Status);
        }

        [Fact]
        public void ResetIndex_WhenAccessIsValid_ShouldReturnOk()
        {
            var namedIndex = "default";
            var accessKey = "local";

            _securityHandlerMock.Setup(x => x.IsAuthenticated(It.IsAny<string>(), It.IsAny<Security.AccessLevel>())).Returns(true);

            var classInstant = SetupMock();
            var result = classInstant.ResetIndex(namedIndex, accessKey) as OkResult;
            Assert.Equal(200, result.StatusCode);
        }
    }
}
