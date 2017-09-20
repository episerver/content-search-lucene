using EPiServer.Search.IndexingService.Configuration;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Configuration
{
    public class NamedIndexElementTest
    {
        [Fact]
        public void GetDirectoryPath_WhenAppDataPathAndNoFrameworkPath_ShouldReturnApp_Data()
        {
            var subject = new NamedIndexElement()
            {
                DirectoryPath = "[appDataPath]"
            };

            Assert.True(subject.GetDirectoryPath().EndsWith("App_Data"));
        }

        [Fact]
        public void GetDirectoryPath_WhenAppDataPathAndFrameworkPath_ShouldReturnFrameworkPath()
        {
            var subject = new NamedIndexElement()
            {
                DirectoryPath = "[appDataPath]",
                GetFrameworkAppDataPath = () => "something"
            };

            Assert.True(subject.GetDirectoryPath().EndsWith("something"));
        }
    }
}
