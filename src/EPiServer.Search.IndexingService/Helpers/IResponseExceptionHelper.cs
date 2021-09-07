using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService.Helpers
{
    public interface IResponseExceptionHelper
    {
        void HandleServiceError(string errorMessage);
        void HandleServiceUnauthorized(string errorMessage);
    }
}
