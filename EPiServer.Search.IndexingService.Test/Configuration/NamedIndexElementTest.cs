using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EPiServer.Search.IndexingService.Configuration;
using EPiServer.TestTools.IntegrationTesting;
using System.Configuration;

namespace EPiServer.Search.IndexingService.Test.Configuration
{
    [TestClass]
    [DeploymentItem(@"EPiServer.Cms.Core.sql", IntegrationTestFiles.SqlOutput)]
    public class NamedIndexElementTest
    {
        [TestMethod]
        public void GetDirectoryPath_WhenAppDataPathAndNoFrameworkPath_ShouldReturnApp_Data()
        {
            var subject = new NamedIndexElement()
            {
                DirectoryPath = "[appDataPath]"
            };

            Assert.IsTrue(subject.GetDirectoryPath().EndsWith("App_Data"));
        }

        [TestMethod]
        public void GetDirectoryPath_WhenAppDataPathAndFrameworkPath_ShouldReturnFrameworkPath()
        {
            var subject = new NamedIndexElement()
            {
                DirectoryPath = "[appDataPath]",
                GetFrameworkAppDataPath = () => "something"
            };

            Assert.IsTrue(subject.GetDirectoryPath().EndsWith("something"));
        }
    }
}
