using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace EPiServer.Search.IndexingService.Configuration
{
    public class NamedIndexesElement
    {
        public string DefaultIndex { get; set; }

        public List<NamedIndexElement> Indexes { get; set; }
    }
}
