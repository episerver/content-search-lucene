using System;
using System.Security.Cryptography.X509Certificates;

namespace EPiServer.Search.Configuration
{
    /// <summary>
    /// Contains settings for a named indexing service
    /// </summary>
    public class IndexingServiceReferenceTransform : IndexingServiceReference
    {
        private X509Certificate2 _clientCertificate;

        internal X509Certificate2 GetClientCertificate()
        {
            var certRef = Certificate;
            if (certRef == null)
                return null;

            if (_clientCertificate != null)
                return _clientCertificate;


            X509Store store = new X509Store(certRef.StoreName, certRef.StoreLocation);
            X509Certificate2Collection certificates = null;
            try
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                certificates = store.Certificates.Find(certRef.X509FindType, certRef.FindValue, false);

                if (certificates.Count == 0)
                {
                    throw new InvalidOperationException("Unable to find client certificate.");
                }
                _clientCertificate = certificates[0];
            }
            finally
            {
                store.Close();
            }

            return _clientCertificate;
        }
    }
}
