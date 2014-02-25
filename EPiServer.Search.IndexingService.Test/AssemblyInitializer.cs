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
            //var packagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\", "packages");

            //if (!Directory.Exists(packagesFolder))
            //{
            //    throw new InvalidOperationException(String.Format("Cant set up database for CMS because nuget package folder was resolved to '{0}' which does not exist", packagesFolder));
            //}

            //var packageRepository = new NuGet.LocalPackageRepository(packagesFolder);

            //var cmsPackage = packageRepository.FindPackagesById("EPiServer.CMS.Core").FirstOrDefault();
            //if (cmsPackage == null)
            //{
            //    throw new InvalidOperationException("Could not get EPiServer.CMS.Core package");
            //}


            //var dbSchema = cmsPackage.GetFiles().Where(f => f.Path.EndsWith("EPiServer.Cms.Core.sql", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            //if (cmsPackage == null)
            //{
            //    throw new InvalidOperationException("Could not get \\tools\\EPiServer.Cms.Core.sql from EPiServer.CMS.Core package");
            //}

            //var sqlFilePath = Path.Combine(packageRepository.Source, String.Format("{0}.{1}", cmsPackage.Id, cmsPackage.Version.ToString()), dbSchema.Path);
            //var destinationFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sql");
            //if (!Directory.Exists(destinationFolder))
            //{
            //    Directory.CreateDirectory(destinationFolder);
            //}

            //File.Copy(sqlFilePath, Path.Combine(destinationFolder, "EPiServer.Cms.Core.sql"), true);

            IntegrationTestHelper.Current = new IntegrationTestHelper();
            IntegrationTestHelper.Current.InitializeDatabase(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None));
        }
    }
}
