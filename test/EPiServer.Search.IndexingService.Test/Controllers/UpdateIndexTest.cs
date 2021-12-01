using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Controllers
{
    [Trait(nameof(EPiServer.Search.IndexingService.Controllers.IndexingController), nameof(EPiServer.Search.IndexingService.Controllers.IndexingController.UpdateIndex))]
    public class UpdateIndexTest : IndexingControllerTestBase
    {
        [Fact]
        public void UpdateIndex_WhenAccessKeyIsInvalid_ShouldReturnUnauthorizedStatusCode()
        {
            var feed = new FeedModel() { };
            var accessKey = "";

            _securityHandlerMock.Setup(x => x.IsAuthenticated(It.IsAny<string>(), It.IsAny<Security.AccessLevel>())).Returns(false);
            _responseExceptionHelperMock.Setup(x => x.HandleServiceUnauthorized(It.IsAny<string>())).Throws(new HttpResponseException() { Status = 401 });

            var classInstant = SetupMock();
            var caughtException = Assert.Throws<HttpResponseException>(() => classInstant.UpdateIndex(accessKey, feed));
            Assert.Equal(401, caughtException.Status);
        }

        [Fact]
        public void UpdateIndex_WhenAccessIsValid_ShouldReturnOk()
        {
            var feed = new FeedModel() { };
            var accessKey = "local";

            _securityHandlerMock.Setup(x => x.IsAuthenticated(It.IsAny<string>(), It.IsAny<Security.AccessLevel>())).Returns(true);

            var classInstant = SetupMock();
            var result = classInstant.UpdateIndex(accessKey, feed) as OkResult;
            Assert.Equal(200, result.StatusCode);
        }
    }
}
