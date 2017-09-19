using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Xunit;

namespace EPiServer.Search.IndexingService
{
    public class IPRangeTest
    {
        [Fact]
        public void TryParseTest1()
        {
            AddressFamily addressFamily = new AddressFamily(); 
            string value = string.Empty;
            IPRange result = null;
            IPRange resultExpected = null;
            bool expected = false;
            bool actual;

            actual = IPRange.TryParse(addressFamily, value, out result);

            Assert.Equal(resultExpected, result);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TryParseTest2()
        {
            AddressFamily addressFamily = AddressFamily.InterNetwork;
            string value = "127.0.0.1";
            IPRange result = null;
            bool expected = true;
            bool actual;

            actual = IPRange.TryParse(addressFamily, value, out result);
            Assert.Equal(expected, actual);

            Assert.False(result.IsInRange(IPAddress.Parse("127.0.0.0")));
            Assert.True(result.IsInRange(IPAddress.Parse("127.0.0.1")));
            Assert.False(result.IsInRange(IPAddress.Parse("127.0.0.2")));
        }

        [Fact]
        public void TryParseTest3()
        {
            AddressFamily addressFamily = AddressFamily.InterNetwork;
            string value = "127.0.0.1/8";
            IPRange result = null;
            bool expected = true;
            bool actual;

            actual = IPRange.TryParse(addressFamily, value, out result);
            Assert.Equal(expected, actual);

            Assert.True(result.IsInRange(IPAddress.Parse("127.0.0.0")));
            Assert.True(result.IsInRange(IPAddress.Parse("127.0.0.255")));
            Assert.True(result.IsInRange(IPAddress.Parse("127.0.0.1")));

            Assert.True(result.IsInRange(IPAddress.Parse("127.0.255.0")));
            Assert.True(result.IsInRange(IPAddress.Parse("127.255.0.0")));
            Assert.True(result.IsInRange(IPAddress.Parse("127.255.255.0")));
            Assert.True(result.IsInRange(IPAddress.Parse("127.255.255.255")));

            Assert.False(result.IsInRange(IPAddress.Parse("0.0.0.0")));
            Assert.False(result.IsInRange(IPAddress.Parse("126.0.0.0")));
            Assert.False(result.IsInRange(IPAddress.Parse("128.0.0.0")));
            Assert.False(result.IsInRange(IPAddress.Parse("255.255.255.255")));

            Assert.False(result.IsInRange(IPAddress.Parse("::1")));
        }


        [Fact]
        public void TryParseTest4()
        {
            AddressFamily addressFamily = AddressFamily.InterNetworkV6;
            string value = "::1/128";
            IPRange result = null;
            bool expected = true;
            bool actual;

            actual = IPRange.TryParse(addressFamily, value, out result);
            Assert.Equal(expected, actual);

            Assert.False(result.IsInRange(IPAddress.Parse("127.0.0.0")));
            Assert.False(result.IsInRange(IPAddress.Parse("127.0.0.255")));
            Assert.False(result.IsInRange(IPAddress.Parse("127.0.0.1")));

            Assert.True(result.IsInRange(IPAddress.Parse("::1")));
            Assert.False(result.IsInRange(IPAddress.Parse("::2")));
            Assert.False(result.IsInRange(IPAddress.Parse("::FFFF:1")));
            Assert.False(result.IsInRange(IPAddress.Parse("::FFFF:1")));
        }

        [Fact]
        public void TryParseTest5()
        {
            AddressFamily addressFamily = AddressFamily.InterNetworkV6;
            string value = "::1/120";
            IPRange result = null;
            bool expected = true;
            bool actual;

            actual = IPRange.TryParse(addressFamily, value, out result);
            Assert.Equal(expected, actual);

            Assert.False(result.IsInRange(IPAddress.Parse("127.0.0.0")));
            Assert.False(result.IsInRange(IPAddress.Parse("127.0.0.255")));
            Assert.False(result.IsInRange(IPAddress.Parse("127.0.0.1")));

            Assert.True(result.IsInRange(IPAddress.Parse("::1")));
            Assert.True(result.IsInRange(IPAddress.Parse("::FF")));
            Assert.False(result.IsInRange(IPAddress.Parse("::FFFF")));
            Assert.False(result.IsInRange(IPAddress.Parse("::100")));
        }

        [Fact]
        public void ClientElementTest1()
        {
            EPiServer.Search.IndexingService.Configuration.ClientElement ce = new EPiServer.Search.IndexingService.Configuration.ClientElement();
            Assert.False(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.False(ce.IsIPAddressAllowed(IPAddress.Parse("::1")));

            ce.IPAddress = "";
            ce.IP6Address = "";
            Assert.False(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.False(ce.IsIPAddressAllowed(IPAddress.Parse("::1")));

            ce.IPAddress = "127.0.0.1";
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.False(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));

            ce.IPAddress = "127.0.0.0/24";
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));
            Assert.False(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.1.0")));

            ce.IPAddress = "127.0.0.1,127.0.1.0";
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.False(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.1.0")));

            ce.IPAddress = "127.0.0.1,,,127.0.1.0";
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.False(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.1.0")));


            ce.IPAddress = "127.0.0.1  127.0.1.0,, ";
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.False(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.1.0")));

            ce.IPAddress = "127.0.0.0/8";
            ce.IP6Address = "::1";
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.1.0")));
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("::1")));

        }

        [Fact]
        public void AllowLocalTest()
        {
            List<IPAddress> localAddresses = new List<IPAddress>();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in nics)
            {
                IPInterfaceProperties properties = nic.GetIPProperties();
                foreach (IPAddressInformation uniCast in properties.UnicastAddresses)
                {
                    localAddresses.Add(uniCast.Address);
                }
            }

            EPiServer.Search.IndexingService.Configuration.ClientElement ce = new EPiServer.Search.IndexingService.Configuration.ClientElement();
            Assert.False(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.False(ce.IsIPAddressAllowed(IPAddress.Parse("::1")));

            ce.AllowLocal = true;
            Assert.True(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            foreach (IPAddress addr in localAddresses)
            {
                Assert.True(ce.IsIPAddressAllowed(addr));
            }
        }

        [Fact]
        public void IPRangeDefinedInConfigurationTest()
        {
            Assert.True(IndexingServiceSettings.ClientElements["accesskey1"].IsIPAddressAllowed(IPAddress.Parse("10.1.74.1")));
        }

    }
}
