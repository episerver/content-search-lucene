 
using System.ComponentModel;
using System.Configuration;
using System.ServiceModel.Configuration;
using System.Security.Cryptography.X509Certificates;
using System;

namespace EPiServer.Search.Configuration
{
    public class NamedIndexingServiceElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("baseUri", IsRequired = true)]
        public Uri BaseUri
        {
            get { return (Uri)base["baseUri"]; }
            set { base["baseUri"] = value; }
        }

        [ConfigurationProperty("accessKey", IsRequired = true)]
        public string AccessKey
        {
            get { return (string)base["accessKey"]; }
            set { base["accessKey"] = value; }
        }

        [ConfigurationProperty("certificate", IsRequired = false)]
        public CertificateReferenceElement Certificate
        {
            get { return (CertificateReferenceElement)base["certificate"]; }
            set { base["certificate"] = value; }
        }


        [ConfigurationProperty("certificateAllowUntrusted", IsRequired = false, DefaultValue = false)]
        public bool CertificateAllowUntrusted
        {
            get { return (bool)base["certificateAllowUntrusted"]; }
            set { base["certificateAllowUntrusted"] = value; }
        }

        private X509Certificate2 _clientCertificate;
        internal X509Certificate2 GetClientCertificate()
        {
            CertificateReferenceElement certRef = Certificate;
            if (certRef == null || !certRef.ElementInformation.IsPresent)
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
 