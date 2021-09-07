using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.DocumentHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper), nameof(EPiServer.Search.IndexingService.Helpers.DocumentHelper.CreateIndex))]
    public class CreateIndexTest : DocumentHelperTestBase
    {
        [Fact]
        public void CreateIndex_ShouldNotNull()
        {
            var classInstant = SetupMock();
            var result = classInstant.CreateIndex("testindex1", new System.IO.DirectoryInfo(string.Format(@"c:\fake\App_Data\{0}\Main", Guid.NewGuid())));
            Assert.NotNull(result);
        }
    }
}
