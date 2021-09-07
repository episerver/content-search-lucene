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
    }
}
