 
using System.ComponentModel;
using System.Configuration;

namespace EPiServer.Search.Configuration
{
    public class NamedIndexingServicesElement : ConfigurationElement
    {
        /// <summary>
        /// Gets and sets the default indexing service name to use
        /// </summary>
        [ConfigurationProperty("defaultService", IsRequired = true)]
        public string DefaultService
        {
            get { return (string)base["defaultService"]; }
            set { base["defaultService"] = value; }
        }

        [ConfigurationProperty("services", IsRequired = true)]
        public NamedIndexingServiceCollection NamedIndexingServices
        {
            get { return (NamedIndexingServiceCollection)base["services"]; }
        }
    }
}
 