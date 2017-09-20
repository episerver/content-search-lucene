using System;
using System.Security.Cryptography.X509Certificates;

namespace EPiServer.Search.Configuration
{
    /// <summary>
    /// Contains settings for a named indexing service
    /// </summary>
    public class IndexingServiceReference
    {
        /// <summary>
        /// Gets or sets the name of the indexing service
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the base uri for the named indexing service
        /// </summary>
        public Uri BaseUri { get; set; }

        /// <summary>
        /// Gets or sets the access key for the indexing service
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Gets or sets the reference of the certificate to use
        /// </summary>
        public CertificateReference Certificate { get; set; }

        /// <summary>
        /// Gets or set if untrusted certificates should be allowed
        /// </summary>
        public bool CertificateAllowUntrusted { get; set; }

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
