using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace EPiServer.Search.IndexingService.Configuration
{
    public class IndexingServiceOptions
    {
        public int MaxDisplayTextLength { get; set; }

        public int MaxHitsForSearchResults { get; set; }

        public bool FIPSCompliant { get; set; }

        public int MaxHitsForReferenceSearch { get; set; }

        public List<ClientElement> Clients { get; set; }

        public NamedIndexesElement NamedIndexes{ get; set; }
    }
}
