using EPiServer.Search.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace EPiServer.Search.Configuration
{
    /// <summary>
    /// Represents the configuration settings for validating an X.509 certificate.
    /// </summary>
    public class CertificateReference
    {
        /// <summary>
        /// Gets or sets a string that specifies the value to search for in the X.509 certificate
        /// store.
        /// </summary>
        public string FindValue { get; set; }

        /// <summary>
        ///  Gets or sets a value that specifies the location of the certificate store the
        ///  client can use to validate the server’s certificate.
        /// </summary>
        public StoreLocation StoreLocation { get; set; } = StoreLocation.LocalMachine;

        /// <summary>
        /// Gets or sets the name of the X.509 certificate store to open.
        /// </summary>
        public StoreName StoreName { get; set; } = StoreName.My;

        /// <summary>
        ///  Gets or sets the type of X.509 search to be executed.
        /// </summary>
        public X509FindType X509FindType { get; set; } = X509FindType.FindBySubjectDistinguishedName;

        /// <summary>
        /// Gets or sets the value of element information
        /// </summary>
        public ElementInformation ElementInformation { get; set; }
    }
}
