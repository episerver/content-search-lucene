using EPiServer.Search.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPiServer.Search.Configuration
{
    public class NamedIndexingServices
    {
        /// <summary>
        /// The name of the default indexing service in
        /// </summary>
        public string DefaultService { get; set; }

        /// <summary>
        /// Contains a list of references for indexing services.
        /// </summary>
        public List<IndexingServiceReference> Services { get; set; }
    }
}
