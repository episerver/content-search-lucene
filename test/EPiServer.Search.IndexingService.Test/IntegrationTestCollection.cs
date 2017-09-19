using EPiServer.Data;
using EPiServer.Search.Internal;
using EPiServer.ServiceLocation;
using EPiServer.TestTools.IntegrationTesting;
using System;
using System.Configuration;
using Xunit;

namespace EPiServer.Search.IndexingService
{
    public static class TestCollection
    {
    }

    public class IntegrationTestCollectionFixture : IDisposable
    {
        public IntegrationTestCollectionFixture()
        {
            IntegrationTestHelper.Current = new IntegrationTestHelper();
            IntegrationTestHelper.Current.InitializeDatabase(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None));
            IntegrationTestHelper.Current.Initialize();
            //IntegrationTestHelper.Current.Initialize(typeof(DataInitialization), typeof(SearchInitialization), typeof(ServiceContainerInitialization));
            //IntegrationTestHelper.Current.Setup(Path.Combine(Environment.CurrentDirectory, "EPiServer.Search.IndexingService.Test.dll.config"));
        }

        public void Dispose()
        {
            IntegrationTestHelper.Current.Cleanup();
        }
    }

    [CollectionDefinition(Name)]
    public class IntegrationTestCollection : ICollectionFixture<IntegrationTestCollectionFixture>
    {
        public const string Name = "IntegrationTests";
    }
}
