using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace EPiServer.Search.IndexingService.Configuration
{
    public class NamedIndexesElement : ConfigurationElement
    {
        [ConfigurationProperty("defaultIndex", IsRequired = true)]
        public string DefaultIndex
        {
            get { return (string)base["defaultIndex"]; }
            set { base["defaultIndex"] = value; }
        }

        [ConfigurationProperty("indexes", IsRequired = true)]
        public NamedIndexCollection NamedIndexes
        {
            get { return (NamedIndexCollection)base["indexes"]; }
        }

    }
}
