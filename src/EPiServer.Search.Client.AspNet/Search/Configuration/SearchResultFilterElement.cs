 
using System.ComponentModel;
using System.Configuration;

namespace EPiServer.Search.Configuration
{
    public class SearchResultFilterElement : ConfigurationElement
    {
        /// <summary>
        /// Gets and sets whether the default behaviour for filtering should be to include results when no provider is configured for the type. Default = false.
        /// </summary>
        [ConfigurationProperty("defaultInclude", IsRequired = true, DefaultValue=false)]
        public bool SearchResultFilterDefaultInclude
        {
            get { return (bool)base["defaultInclude"]; }
            set { base["defaultInclude"] = value; }
        }

        [ConfigurationProperty("providers", IsRequired = false)]
        public ProviderSettingsCollection Providers
        {
            get { return (ProviderSettingsCollection)base["providers"]; }
        }
    }
}
 