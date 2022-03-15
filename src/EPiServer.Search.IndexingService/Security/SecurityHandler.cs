using EPiServer.Search.IndexingService.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EPiServer.Search.IndexingService.Security
{
    public class SecurityHandler : ISecurityHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ClientElementHandler _clientElementHandler;

        public SecurityHandler(IHttpContextAccessor httpContextAccessor,
            ClientElementHandler clientElementHandler)
        {
            _httpContextAccessor = httpContextAccessor;
            _clientElementHandler = clientElementHandler;
        }

        public bool IsAuthenticated(string accessKey, AccessLevel accessLevel)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Request for authorization for access key '{0}'", accessKey));

            //Always fail if no client access key is found in the request
            if (string.IsNullOrEmpty(accessKey))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.LogError("No access key found. Access denied.");
                return false;
            }

            //Check if the access key exists and get the client element
            if (!IndexingServiceSettings.ClientElements.TryGetValue(accessKey, out var elem))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.LogError(string.Format("The access key: '{0}' was not found for configured clients. Access denied.", accessKey));
                return false;
            }

            //Check level.
            if (elem.ReadOnly && accessLevel == AccessLevel.Modify)
            {
                IndexingServiceSettings.IndexingServiceServiceLog.LogError(string.Format("Modify request for access key '{0}' failed. Only read access", accessKey));
                return false;
            }

            //Try to authenticate this request by configured client IP
            var remoteIpAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress;
            if (remoteIpAddress.IsIPv4MappedToIPv6)
            {
                remoteIpAddress = remoteIpAddress.MapToIPv4();
            }

            if (!_clientElementHandler.IsIPAddressAllowed(elem, remoteIpAddress))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.LogError(string.Format("No match for client IP {0}. Access denied for access key {1}.", remoteIpAddress, accessKey));
                return false;
            }

            IndexingServiceSettings.IndexingServiceServiceLog.LogDebug(string.Format("Request for authorization for access key '{0}' succeded", accessKey));

            return true;
        }
    }
}
