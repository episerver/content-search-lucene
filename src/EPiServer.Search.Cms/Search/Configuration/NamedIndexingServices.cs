using System.Collections.Generic;

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
