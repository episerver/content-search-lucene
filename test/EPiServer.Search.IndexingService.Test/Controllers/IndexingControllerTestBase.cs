using EPiServer.Search.IndexingService.Helpers;
using EPiServer.Search.IndexingService.Security;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPiServer.Search.IndexingService.Test.Controllers
{
    public class IndexingControllerTestBase
    {
        protected readonly Mock<IResponseExceptionHelper> _responseExceptionHelperMock;
        protected readonly Mock<IIndexingServiceSettings> _indexingServiceSettingsMock;
        protected readonly Mock<IIndexingServiceHandler> _indexingServiceHandlerMock;
        protected readonly Mock<ISecurityHandler> _securityHandlerMock;
        protected readonly Mock<ILogger<IndexingService.Controllers.IndexingController>> _loggerMock;
        public IndexingControllerTestBase()
        {
            _responseExceptionHelperMock = new Mock<IResponseExceptionHelper>();
            _indexingServiceSettingsMock = new Mock<IIndexingServiceSettings>();
            _indexingServiceHandlerMock = new Mock<IIndexingServiceHandler>();
            _securityHandlerMock = new Mock<ISecurityHandler>();

            _loggerMock = new Mock<ILogger<IndexingService.Controllers.IndexingController>>();
        }

        public IndexingService.Controllers.IndexingController SetupMock()
        {
            return new IndexingService.Controllers.IndexingController(
                _securityHandlerMock.Object,
                _indexingServiceHandlerMock.Object,
                _indexingServiceSettingsMock.Object,
                _responseExceptionHelperMock.Object,
                _loggerMock.Object);
        }
    }
}
