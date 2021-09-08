using System;
using System.Security.Cryptography;
using System.ServiceModel;
using EPiServer.Search.IndexingService.Configuration;
using Microsoft.AspNetCore.Http;

namespace EPiServer.Search.IndexingService.Security
{
    public class SecurityHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ClientElementHandler _clientElementHandler;

        public SecurityHandler(IHttpContextAccessor httpContextAccessor,
            ClientElementHandler clientElementHandler)
        {
            _httpContextAccessor = httpContextAccessor;
            _clientElementHandler = clientElementHandler;
        }

        protected internal virtual bool IsAuthenticated(string accessKey, AccessLevel accessLevel)
        {
            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Request for authorization for access key '{0}'", accessKey));

            //Always fail if no client access key is found in the request
            if (String.IsNullOrEmpty(accessKey))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Error("No access key found. Access denied.");
                return false;
            }

            //Check if the access key exists and get the client element
            ClientElement elem;
            if (!IndexingServiceSettings.ClientElements.TryGetValue(accessKey, out elem))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Error(String.Format("The access key: '{0}' was not found for configured clients. Access denied.", accessKey));
                return false;
            }

            //Check level.
            if (elem.ReadOnly && accessLevel == AccessLevel.Modify)
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Error(String.Format("Modify request for access key '{0}' failed. Only read access", accessKey));
                return false;
            }

            //Try to authenticate this request by configured client IP
            var remoteIpAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress;

            if (!_clientElementHandler.IsIPAddressAllowed(elem, remoteIpAddress))
            {
                IndexingServiceSettings.IndexingServiceServiceLog.Error(string.Format("No match for client IP {0}. Access denied for access key {1}.", remoteIpAddress, accessKey));
                return false;
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Request for authorization for access key '{0}' succeded", accessKey));

            return true;
        }
    }
}
