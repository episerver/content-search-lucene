using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace EPiServer.Search.IndexingService.Configuration
{
    public class IndexingServiceSection : ConfigurationSection
    {
        [ConfigurationProperty("maxDisplayTextLength", IsRequired = false, DefaultValue = 500)]
        public int MaxDisplayTextLength
        {
            get { return (int)base["maxDisplayTextLength"]; }
            set { base["maxDisplayTextLength"] = value; }
        }

        [ConfigurationProperty("autoUpdateVirtualPath", IsRequired = false, DefaultValue = true)]
        [Obsolete("AutoUpdateVirtualPath is now decided on a per request basis by checking for an attribute.")]
        public bool AutoUpdateVirtualPath
        {
            get { return (bool)base["autoUpdateVirtualPath"]; }
            set { base["autoUpdateVirtualPath"] = value; }
        }

        [ConfigurationProperty("maxHitsForSearchResults", IsRequired = false, DefaultValue = 1000)]
        public int MaxHitsForSearchResults
        {
            get { return (int)base["maxHitsForSearchResults"]; }
            set { base["maxHitsForSearchResults"] = value; }
        }

        [ConfigurationProperty("maxHitsForReferenceSearch", IsRequired = false, DefaultValue = 10000)]
        public int MaxHitsForReferenceSearch
        {
            get { return (int)base["maxHitsForReferenceSearch"]; }
            set { base["maxHitsForReferenceSearch"] = value; }
        }

        [ConfigurationProperty("clients", IsRequired = true)]
        public ClientCollection Clients
        {
            get { return (ClientCollection)base["clients"]; }
        }

        [ConfigurationProperty("namedIndexes", IsRequired = true)]
        public NamedIndexesElement NamedIndexesElement
        {
            get { return (NamedIndexesElement)base["namedIndexes"]; }
            set { base["namedIndexes"] = value; }
        }
    }
}
