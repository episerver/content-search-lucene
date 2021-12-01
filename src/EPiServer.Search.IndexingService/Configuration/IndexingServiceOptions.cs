using System.Collections.Generic;

namespace EPiServer.Search.IndexingService.Configuration
{
    public class IndexingServiceOptions
    {
        public int MaxDisplayTextLength { get; set; } = 500;

        public int MaxHitsForSearchResults { get; set; } = 1000;

        public bool FIPSCompliant { get; set; }

        public int MaxHitsForReferenceSearch { get; set; } = 10000;

        public List<ClientElement> Clients { get; set; }

        public NamedIndexesElement NamedIndexes { get; set; }
    }
}
