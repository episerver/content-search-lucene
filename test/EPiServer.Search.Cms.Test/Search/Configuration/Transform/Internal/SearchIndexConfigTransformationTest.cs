using EPiServer.Core;
using EPiServer.Framework.Configuration;
using Moq;
using System.Collections.Specialized;
using System.Linq;
using Xunit;

namespace EPiServer.Search.Configuration.Transform.Internal
{
    public class SearchIndexConfigTransformationTest
    {
        [Fact]
        public void Transform_IfNamedIndexAppSettingExist_ShouldSetOnConfig()
        {
            var namedIndex = "namedindex";
            var appSettings = new NameValueCollection();
            appSettings.Add("EPiCmsNamedIndex", namedIndex);

            var configurationSource = new Mock<IConfigurationSource>();
            configurationSource.Setup(c => c.Get<object>("appSettings")).Returns(appSettings);

            var searchIndexConfig = new SearchIndexConfig();
            var subject = new SearchIndexConfigTransformation(searchIndexConfig, configurationSource.Object);
            subject.Transform();

            Assert.Equal(namedIndex, searchIndexConfig.CMSNamedIndex);
            Assert.Equal(namedIndex, searchIndexConfig.NamedIndexes.Single());
        }

        [Fact]
        public void Transform_IfNoNamedIndexAppSettingExist_ShouldNotSetOnConfig()
        {
            var appSettings = new NameValueCollection();

            var configurationSource = new Mock<IConfigurationSource>();
            configurationSource.Setup(c => c.Get<object>("appSettings")).Returns(appSettings);

            var searchIndexConfig = new SearchIndexConfig();
            var subject = new SearchIndexConfigTransformation(searchIndexConfig, configurationSource.Object);
            subject.Transform();

            Assert.Null(searchIndexConfig.CMSNamedIndex);
            Assert.Null(searchIndexConfig.NamedIndexes);
        }

        [Fact]
        public void Transform_IfNamedIndexServiceAppSettingExist_ShouldSetOnConfig()
        {
            var namedIndex = "namedindex";
            var appSettings = new NameValueCollection();
            appSettings.Add("EPiCmsNamedIndexingService", namedIndex);

            var configurationSource = new Mock<IConfigurationSource>();
            configurationSource.Setup(c => c.Get<object>("appSettings")).Returns(appSettings);

            var searchIndexConfig = new SearchIndexConfig();
            var subject = new SearchIndexConfigTransformation(searchIndexConfig, configurationSource.Object);
            subject.Transform();

            Assert.Equal(namedIndex, searchIndexConfig.NamedIndexingService);
        }
    }
}
