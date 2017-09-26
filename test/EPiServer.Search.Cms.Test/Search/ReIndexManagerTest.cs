using System;
using System.Linq;
using EPiServer.Search.Internal;
using Moq;
using Xunit;

namespace EPiServer.Search
{
    public class ReIndexManagerTest
    {
        [Fact]
        public void ReIndex_ShouldCallReIndexOnService()
        {
            var service = new Mock<IReIndexable>();
            var reIndexManager = ReIndexManager(service.Object);

            service.Verify(x => x.ReIndex(), Times.Never());

            reIndexManager.ReIndex();

            service.Verify(x => x.ReIndex(), Times.Once());
        }

        [Fact]
        public void ReIndex_WithMultipleServices_ShouldCallReIndexOnAllService()
        {
            var service1 = new Mock<IReIndexable>();
            var service2 = new Mock<IReIndexable>();
            var reIndexManager = ReIndexManager(service1.Object, service2.Object);

            service1.Verify(x => x.ReIndex(), Times.Never());
            service1.Verify(x => x.ReIndex(), Times.Never());

            reIndexManager.ReIndex();

            service1.Verify(x => x.ReIndex(), Times.Once());
            service2.Verify(x => x.ReIndex(), Times.Once());
        }

        [Fact]
        public void ReIndex_WhenServiceThrows_ShouldNotThrowException()
        {
            var service = new Mock<IReIndexable>();
            service.Setup(x => x.ReIndex()).Throws<InvalidOperationException>();

            var reIndexManager = ReIndexManager(service.Object);

            reIndexManager.ReIndex();

            Assert.True(true);
        }

        [Fact]
        public void ReIndex_WhenOneOfMultipleServicesThrows_ShouldCallReIndexOnNextService()
        {
            var service1 = new Mock<IReIndexable>();
            service1.Setup(x => x.ReIndex()).Throws<InvalidOperationException>();
            var service2 = new Mock<IReIndexable>();
            var reIndexManager = ReIndexManager(service1.Object, service2.Object);

            reIndexManager.ReIndex();

            service2.Verify(x => x.ReIndex(), Times.Once());
        }

        private static ReIndexManager ReIndexManager(params IReIndexable[] reIndexable)
        {
            var searchHandler = new Mock<SearchHandler>(null, null, null).Object;
            return new ReIndexManager(searchHandler, reIndexable ?? Enumerable.Empty<IReIndexable>());
        }
    }
}
