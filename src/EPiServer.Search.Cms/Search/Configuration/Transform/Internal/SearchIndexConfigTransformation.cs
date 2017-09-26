using EPiServer.Configuration.Transform.Internal;
using EPiServer.Core;
using EPiServer.Framework.Configuration;

namespace EPiServer.Search.Configuration.Transform.Internal
{
    public class SearchIndexConfigTransformation : IConfigurationTransform
    {
        private readonly IConfigurationSource _configSource;
        private readonly SearchIndexConfig _searchIndexConfig;

        public SearchIndexConfigTransformation(SearchIndexConfig searchIndexConfig, IConfigurationSource configSource)
        {
            _searchIndexConfig = searchIndexConfig;
            _configSource = configSource;
        }

        public void Transform()
        {
            var cmsNamedIndex = _configSource.GetSetting("EPiCmsNamedIndex");
            if (!string.IsNullOrEmpty(cmsNamedIndex))
            {
                _searchIndexConfig.CMSNamedIndex = cmsNamedIndex;
                _searchIndexConfig.NamedIndexes = new System.Collections.ObjectModel.Collection<string>();
                _searchIndexConfig.NamedIndexes.Add(cmsNamedIndex);
            }
                
            var cmsNamedIndexService = _configSource.GetSetting("EPiCmsNamedIndexingService");
            if (!string.IsNullOrEmpty(cmsNamedIndexService))
                _searchIndexConfig.NamedIndexingService = cmsNamedIndexService;
        }
    }
}
 