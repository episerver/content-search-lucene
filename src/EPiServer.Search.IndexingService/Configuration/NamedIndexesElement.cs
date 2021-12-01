using System.Collections.Generic;

namespace EPiServer.Search.IndexingService.Configuration
{
    public class NamedIndexesElement
    {
        public string DefaultIndex { get; set; }

        public List<NamedIndexElement> Indexes { get; set; }
    }
}
