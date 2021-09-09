using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService.Security
{
    public interface ISecurityHandler
    {
        bool IsAuthenticated(string accessKey, AccessLevel accessLevel);
    }
}
