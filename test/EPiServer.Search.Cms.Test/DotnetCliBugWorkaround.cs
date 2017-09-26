using Xunit;

namespace EPiServer
{
    public class DotnetCliBugWorkaround
    {
        [Fact]
        [Trait("Category", "PDF")]
        public void TestWithTraitToWorkAroundDotnetCliTestFilterBug()
        {
            // Test runner throws exception when filtering on unknown traits
            // See https://github.com/Microsoft/vstest/pull/845
            // and https://github.com/xunit/xunit/issues/1314
        }
    }
}
