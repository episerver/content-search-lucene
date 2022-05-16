using System;
using System.Threading;
using EPiServer.Search.IndexingService.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    public class LuceneHelperTestBase
    {
        protected readonly Mock<IFeedHelper> _feedHelperMock;
        protected readonly Mock<ICommonFunc> _commonFuncMock;
        protected readonly Mock<IResponseExceptionHelper> _responseExceptionHelperMock;
        protected readonly Mock<IDocumentHelper> _documentHelperMock;
        protected readonly Mock<ILogger<IndexingService.Helpers.LuceneHelper>> _loggerMock;
        public LuceneHelperTestBase()
        {
            IndexingServiceSettings.ReaderWriterLocks.TryAdd("testindex1", new ReaderWriterLockSlim());
            IndexingServiceSettings.ReaderWriterLocks.TryAdd("testindex1" + IndexingServiceSettings.RefIndexSuffix, new ReaderWriterLockSlim());
            IndexingServiceSettings.ReaderWriterLocks.TryAdd("testindex2", new ReaderWriterLockSlim());

            _feedHelperMock = new Mock<IFeedHelper>();
            _commonFuncMock = new Mock<ICommonFunc>();
            _responseExceptionHelperMock = new Mock<IResponseExceptionHelper>();
            _documentHelperMock = new Mock<IDocumentHelper>();
            _loggerMock = new Mock<ILogger<IndexingService.Helpers.LuceneHelper>>();
        }

        public EPiServer.Search.IndexingService.Helpers.LuceneHelper SetupMock()
        {
            return new EPiServer.Search.IndexingService.Helpers.LuceneHelper(
                _feedHelperMock.Object,
                _responseExceptionHelperMock.Object,
                _commonFuncMock.Object,
                _documentHelperMock.Object,
                _loggerMock.Object);
        }
    }
}
