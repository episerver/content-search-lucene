using System;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using EPiServer.Search.IndexingService.Configuration;

namespace EPiServer.Search.IndexingService.Security
{
    public class SecurityHandler
    {
        private static SecurityHandler _instance = new SecurityHandler();

        static SecurityHandler()
        {
        }

        protected SecurityHandler()
        {
        }

        public static SecurityHandler Instance
        {
            get
            {
                return _instance;
            }
            set
            {
                _instance = value;
            }
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
            OperationContext context = OperationContext.Current;
            if (context != null)
            {
                MessageProperties messageProperties = context.IncomingMessageProperties;
                RemoteEndpointMessageProperty endpointProperty = messageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;

                if (!elem.IsIPAddressAllowed(System.Net.IPAddress.Parse(endpointProperty.Address)))
                {
                    IndexingServiceSettings.IndexingServiceServiceLog.Error(string.Format("No match for client IP {0}. Access denied for access key {1}.", endpointProperty.Address, accessKey));
                    return false;
                }
            }

            IndexingServiceSettings.IndexingServiceServiceLog.Debug(String.Format("Request for authorization for access key '{0}' succeded", accessKey));

            return true;
        }
    }
}
