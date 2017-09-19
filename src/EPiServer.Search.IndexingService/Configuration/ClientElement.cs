using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Net;
using System.Net.NetworkInformation;

namespace EPiServer.Search.IndexingService.Configuration
{
    public class ClientElement : ConfigurationElement
    {
        object _lockObj = new object();
        List<IPAddress> _localIps = null;
        List<IPRange> _ip6Ranges = null;
        List<IPRange> _ip4Ranges = null;

        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("description", IsRequired = false)]
        public string Description
        {
            get { return (string)base["description"]; }
            set { base["description"] = value; }
        }

        [ConfigurationProperty("ipAddress", IsRequired = false)]
        public string IPAddress
        {
            get { return (string)base["ipAddress"]; }
            set
            {
                base["ipAddress"] = value;
                _ip4Ranges = ParseIPRangeList(System.Net.Sockets.AddressFamily.InterNetwork, value);
            }
        }

        [ConfigurationProperty("ip6Address", IsRequired = false)]
        public string IP6Address
        {
            get { return (string)base["ip6Address"]; }
            set
            {
                base["ip6Address"] = value;
                _ip6Ranges = ParseIPRangeList(System.Net.Sockets.AddressFamily.InterNetworkV6, value);
            }
        }

        [ConfigurationProperty("allowLocal", IsRequired = false, DefaultValue = false)]
        public bool AllowLocal
        {
            get { return (bool)base["allowLocal"]; }
            set
            {
                base["allowLocal"] = value;
            }
        }

        [ConfigurationProperty("readonly", IsRequired = true)]
        public bool ReadOnly
        {
            get { return (bool)base["readonly"]; }
            set { base["readonly"] = value; }
        }

        private List<IPRange> ParseIPRangeList(System.Net.Sockets.AddressFamily addressFamily, string list)
        {
            List<IPRange> result = new List<IPRange>();
            string[] ranges = list.Split(',', ' ');
            foreach (string range in ranges)
            {
                string r = range.Trim();
                IPRange ipr;
                if (r.Length > 0 && IPRange.TryParse(addressFamily, r, out ipr))
                {
                    result.Add(ipr);
                }
            }

            return result;
        }

        private List<IPAddress> GetLocalAddresses()
        {
            List<IPAddress> localIps = new List<IPAddress>();
            
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in nics)
            {
                IPInterfaceProperties properties = nic.GetIPProperties();
                foreach (IPAddressInformation uniCast in properties.UnicastAddresses)
                {
                    localIps.Add(uniCast.Address);
                }
            }

            return localIps;
        }

        internal bool IsIPAddressAllowed(IPAddress ipAddress)
        {
            if (AllowLocal)
            {
                if (_localIps == null)
                {
                    lock (_lockObj)
                    {
                        if (_localIps == null)
                        {
                            _localIps = GetLocalAddresses();
                        }
                    }
                }

                // Check if the IP is local and AllowLocal is enabled
                if (_localIps != null)
                {
                    foreach (IPAddress addr in _localIps)
                    {
                        if (addr.Equals(ipAddress))
                            return true;
                    }
                }
            }

            // Check IPv4 list
            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                if (_ip4Ranges == null)
                {
                    lock (_lockObj)
                    {
                        if (_ip4Ranges == null)
                        {
                            _ip4Ranges = ParseIPRangeList(System.Net.Sockets.AddressFamily.InterNetwork, IPAddress);
                        }
                    }
                }

                if (_ip4Ranges != null)
                {
                    foreach (IPRange r in _ip4Ranges)
                    {
                        if (r.IsInRange(ipAddress))
                            return true;
                    }
                }
            }
            // Check IPv6 list
            else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (_ip6Ranges == null)
                {
                    lock (_lockObj)
                    {
                        if (_ip6Ranges == null)
                        {
                            _ip6Ranges = ParseIPRangeList(System.Net.Sockets.AddressFamily.InterNetworkV6, IP6Address);
                        }
                    }
                }


                if (_ip6Ranges != null)
                {
                    foreach (IPRange r in _ip6Ranges)
                    {
                        if (r.IsInRange(ipAddress))
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
