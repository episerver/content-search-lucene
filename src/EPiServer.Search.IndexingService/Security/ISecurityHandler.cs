namespace EPiServer.Search.IndexingService.Security
{
    public interface ISecurityHandler
    {
        bool IsAuthenticated(string accessKey, AccessLevel accessLevel);
    }
}
