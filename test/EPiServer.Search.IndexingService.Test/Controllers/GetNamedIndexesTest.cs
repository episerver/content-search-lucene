using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Controllers
{
    [Trait(nameof(EPiServer.Search.IndexingService.Controllers.IndexingController), nameof(EPiServer.Search.IndexingService.Controllers.IndexingController.GetNamedIndexes))]
    public class GetNamedIndexesTest : IndexingControllerTestBase
    {
        [Fact]
        public void GetNamedIndexes_WhenAccessKeyIsInvalid_ShouldReturnUnauthorizedStatusCode()
        {
            var accessKey = "";

            _securityHandlerMock.Setup(x => x.IsAuthenticated(It.IsAny<string>(), It.IsAny<Security.AccessLevel>())).Returns(false);
            _responseExceptionHelperMock.Setup(x => x.HandleServiceUnauthorized(It.IsAny<string>())).Throws(new HttpResponseException() { Status = 401 });

            var classInstant = SetupMock();
            var caughtException = Assert.Throws<HttpResponseException>(() => classInstant.GetNamedIndexes(accessKey));
            Assert.Equal(401, caughtException.Status);
        }

        [Fact]
        public void GetNamedIndexes_WhenAccessIsValid_ShouldReturnOk()
        {
            var accessKey = "local";
            var feed = new FeedModel();
            var feeditem = new FeedItemModel()
            {
                Title = "default",
            };
            feed.Items = new List<FeedItemModel>() { feeditem };

            _securityHandlerMock.Setup(x => x.IsAuthenticated(It.IsAny<string>(), It.IsAny<Security.AccessLevel>())).Returns(true);
            _indexingServiceHandlerMock.Setup(x => x.GetNamedIndexes()).Returns(feed);

            var classInstant = SetupMock();
            var result = classInstant.GetNamedIndexes(accessKey) as OkObjectResult;
            var resultFeed = (FeedModel)result.Value;
            Assert.Equal("default", resultFeed.Items.ToList()[0].Title);
        }
    }

}
