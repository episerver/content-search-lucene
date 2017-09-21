using EPiServer.TestTools.IntegrationTesting;
using System;
using Xunit;

namespace EPiServer.Search.IndexingService
{
    public static class TestCollection { }

    public class IntegrationTestCollectionFixture : IDisposable
    {
        public IntegrationTestCollectionFixture()
        {
            IntegrationTestHelper.Current = new IntegrationTestHelper();
            IntegrationTestHelper.Current.Setup(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            IntegrationTestHelper.Current.Initialize();
        }

        public void Dispose()
        {
            IntegrationTestHelper.Current.Uninitialize();
            IntegrationTestHelper.Current.Cleanup();
        }
    }

    [CollectionDefinition(Name)]
    public class IntegrationTestCollection : ICollectionFixture<IntegrationTestCollectionFixture>
    {
        public const string Name = "IntegrationTests";
    }
}
