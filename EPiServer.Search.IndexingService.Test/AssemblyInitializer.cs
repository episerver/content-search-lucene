using EPiServer.TestTools.IntegrationTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace EPiServer.Search.IndexingService.Test
{
    [TestClass]
    public class AssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext testContext)
        {
            IntegrationTestHelper.Current = new IntegrationTestHelper();
            IntegrationTestHelper.Current.InitializeDatabase(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None));
        }

    }
}
