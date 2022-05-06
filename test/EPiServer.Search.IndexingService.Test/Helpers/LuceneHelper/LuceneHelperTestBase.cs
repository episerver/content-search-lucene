using System;
using System.Threading;
using EPiServer.Search.IndexingService.Helpers;
using Moq;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    public class LuceneHelperTestBase
    {
        protected readonly Mock<IFeedHelper> _feedHelperMock;
        protected readonly Mock<ICommonFunc> _commonFuncMock;
        protected readonly Mock<IResponseExceptionHelper> _responseExceptionHelperMock;
        protected readonly Mock<IDocumentHelper> _documentHelperMock;
        public LuceneHelperTestBase()
        {
            IndexingServiceSettings.ReaderWriterLocks.TryAdd("testindex1", new ReaderWriterLockSlim());
            IndexingServiceSettings.ReaderWriterLocks.TryAdd("testindex1" + IndexingServiceSettings.RefIndexSuffix, new ReaderWriterLockSlim());
            IndexingServiceSettings.ReaderWriterLocks.TryAdd("testindex2", new ReaderWriterLockSlim());

            _feedHelperMock = new Mock<IFeedHelper>();
            _commonFuncMock = new Mock<ICommonFunc>();
            _responseExceptionHelperMock = new Mock<IResponseExceptionHelper>();
            _documentHelperMock = new Mock<IDocumentHelper>();
        }

        public EPiServer.Search.IndexingService.Helpers.LuceneHelper SetupMock()
        {
            return new EPiServer.Search.IndexingService.Helpers.LuceneHelper(
                _feedHelperMock.Object,
                _responseExceptionHelperMock.Object,
                _commonFuncMock.Object,
                _documentHelperMock.Object);
        }
    }
}
