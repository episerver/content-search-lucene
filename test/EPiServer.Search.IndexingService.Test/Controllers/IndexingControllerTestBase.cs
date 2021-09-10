using EPiServer.Logging.Compatibility;
using EPiServer.Search.IndexingService.Helpers;
using EPiServer.Search.IndexingService.Security;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Controllers
{
    public class IndexingControllerTestBase
    {
        protected readonly Mock<IResponseExceptionHelper> _responseExceptionHelperMock;
        protected readonly Mock<IIndexingServiceSettings> _indexingServiceSettingsMock;
        protected readonly Mock<IIndexingServiceHandler> _indexingServiceHandlerMock;
        protected readonly Mock<ISecurityHandler> _securityHandlerMock;
        public IndexingControllerTestBase()
        {
            _responseExceptionHelperMock = new Mock<IResponseExceptionHelper>();
            _indexingServiceSettingsMock = new Mock<IIndexingServiceSettings>();
            _indexingServiceHandlerMock = new Mock<IIndexingServiceHandler>();
            _securityHandlerMock = new Mock<ISecurityHandler>();

            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;
        }

        public EPiServer.Search.IndexingService.Controllers.IndexingController SetupMock()
        {
            return new EPiServer.Search.IndexingService.Controllers.IndexingController(
                _securityHandlerMock.Object,
                _indexingServiceHandlerMock.Object,
                _indexingServiceSettingsMock.Object,
                _responseExceptionHelperMock.Object);
        }
    }
}
