using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Net;
using System.Net.NetworkInformation;

namespace EPiServer.Search.IndexingService.Configuration
{
    public class ClientElement
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string IPAddress { get; set; }

        public string IP6Address { get; set; }

        public bool AllowLocal { get; set; }

        public bool ReadOnly { get; set; }
    }

    public class ClientElementHandler
    {
        object _lockObj = new object();
        List<IPAddress> _localIps = null;
        List<IPRange> _ip6Ranges = null;
        List<IPRange> _ip4Ranges = null;

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

        internal bool IsIPAddressAllowed(ClientElement clientElement, IPAddress ipAddress)
        {
            if (clientElement.AllowLocal)
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
                            _ip4Ranges = ParseIPRangeList(System.Net.Sockets.AddressFamily.InterNetwork, clientElement.IPAddress);
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
                            _ip6Ranges = ParseIPRangeList(System.Net.Sockets.AddressFamily.InterNetworkV6, clientElement.IP6Address);
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