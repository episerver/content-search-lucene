namespace EPiServer.Search.IndexingService.Helpers
{
    public interface IResponseExceptionHelper
    {
        void HandleServiceError(string errorMessage);
        void HandleServiceUnauthorized(string errorMessage);
    }
}
