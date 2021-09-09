using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Controllers
{
    [Trait(nameof(EPiServer.Search.IndexingService.Controllers.IndexingController), nameof(EPiServer.Search.IndexingService.Controllers.IndexingController.GetSearchResultsJson))]
    public class GetSearchResultsJsonTest:IndexingControllerTestBase
    {
        [Fact]
        public void GetSearchResultsJson_WhenAccessKeyIsInvalid_ShouldReturnUnauthorizedStatusCode()
        {
            var namedIndex = "";
            var accessKey = "";
            var q = "";

            _securityHandlerMock.Setup(x => x.IsAuthenticated(It.IsAny<string>(), It.IsAny<Security.AccessLevel>())).Returns(false);
            _responseExceptionHelperMock.Setup(x => x.HandleServiceUnauthorized(It.IsAny<string>())).Throws(new HttpResponseException() { Status = 401 });

            var classInstant = SetupMock();
            var caughtException = Assert.Throws<HttpResponseException>(() => classInstant.GetSearchResultsJson(q, namedIndex, "0", "1", accessKey));
            Assert.Equal(401, caughtException.Status);
        }

        [Fact]
        public void GetSearchResultsJson_WhenAccessIsValid_ShouldReturnOk()
        {
            var namedIndex = "default";
            var accessKey = "local";
            var q = string.Format("{0}:{1}", IndexingServiceSettings.IdFieldName, 1);
            var feed = new FeedModel();
            feed.AttributeExtensions.Add(IndexingServiceSettings.SyndicationFeedAttributeNameVersion, "EPiServer.Search v.1.0.0.0");
            var feeditem = new FeedItemModel()
            {
                Id = "1",
                Title = "Title",
                DisplayText = "Body"
            };
            feed.Items = new List<FeedItemModel>() { feeditem };

            _securityHandlerMock.Setup(x => x.IsAuthenticated(It.IsAny<string>(), It.IsAny<Security.AccessLevel>())).Returns(true);
            _indexingServiceHandlerMock
                .Setup(x => x.GetSearchResults(
                    It.IsAny<string>(), 
                    It.Is<string>(y => !string.IsNullOrEmpty(y)), 
                    It.Is<int>(y => y >= 0), 
                    It.Is<int>(y => y > 0)))
                .Returns(feed);

            var classInstant = SetupMock();
            var result = classInstant.GetSearchResultsJson(q, namedIndex, "0", "1", accessKey) as OkObjectResult;
            var resultFeed = (FeedModel)result.Value;
            Assert.Equal("Title", resultFeed.Items.ToList()[0].Title);
        }
    }
}
