using Microsoft.Extensions.Logging;

namespace EPiServer.Search.IndexingService.Helpers
{
    public class ResponseExceptionHelper : IResponseExceptionHelper
    {
        public void HandleServiceError(string errorMessage)
        {
            //Log, fire event and respond with status code 500
            IndexingServiceSettings.IndexingServiceServiceLog.LogError(errorMessage);
            throw new HttpResponseException()
            {
                Value = new { error = errorMessage },
                Status = 500
            };
        }

        public void HandleServiceUnauthorized(string errorMessage)
        {
            //Log, fire event and respond with status code 500
            IndexingServiceSettings.IndexingServiceServiceLog.LogError(errorMessage);
            throw new HttpResponseException()
            {
                Value = new { error = errorMessage },
                Status = 401
            };
        }
    }
}
