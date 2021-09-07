using EPiServer.Logging.Compatibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService
{
    public interface IIndexingServiceSettings
    {
        static ILog IndexingServiceServiceLog { get; set; }
    }
}
