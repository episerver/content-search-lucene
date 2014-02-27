using EPiServer.Search.IndexingService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Web;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using EPiServer.TestTools.IntegrationTesting;

namespace EPiServer.Search.IndexingService.Test
{
    
    
    /// <summary>
    ///This is a test class for IPRangeTest and is intended
    ///to contain all IPRangeTest Unit Tests
    ///</summary>
    [TestClass()]
    [DeploymentItem(@"EPiServer.Cms.Core.sql", IntegrationTestFiles.SqlOutput)]
    public class IPRangeTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for TryParse
        ///</summary>
        [TestMethod()]
        public void TryParseTest1()
        {
            AddressFamily addressFamily = new AddressFamily(); 
            string value = string.Empty;
            IPRange result = null;
            IPRange resultExpected = null;
            bool expected = false;
            bool actual;

            actual = IPRange.TryParse(addressFamily, value, out result);

            Assert.AreEqual(resultExpected, result);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for TryParse
        ///</summary>
        [TestMethod()]
        public void TryParseTest2()
        {
            AddressFamily addressFamily = AddressFamily.InterNetwork;
            string value = "127.0.0.1";
            IPRange result = null;
            bool expected = true;
            bool actual;

            actual = IPRange.TryParse(addressFamily, value, out result);
            Assert.AreEqual(expected, actual);

            Assert.IsFalse(result.IsInRange(IPAddress.Parse("127.0.0.0")));
            Assert.IsTrue(result.IsInRange(IPAddress.Parse("127.0.0.1")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("127.0.0.2")));
        }

        /// <summary>
        ///A test for TryParse
        ///</summary>
        [TestMethod()]
        public void TryParseTest3()
        {
            AddressFamily addressFamily = AddressFamily.InterNetwork;
            string value = "127.0.0.1/8";
            IPRange result = null;
            bool expected = true;
            bool actual;

            actual = IPRange.TryParse(addressFamily, value, out result);
            Assert.AreEqual(expected, actual);

            Assert.IsTrue(result.IsInRange(IPAddress.Parse("127.0.0.0")));
            Assert.IsTrue(result.IsInRange(IPAddress.Parse("127.0.0.255")));
            Assert.IsTrue(result.IsInRange(IPAddress.Parse("127.0.0.1")));

            Assert.IsTrue(result.IsInRange(IPAddress.Parse("127.0.255.0")));
            Assert.IsTrue(result.IsInRange(IPAddress.Parse("127.255.0.0")));
            Assert.IsTrue(result.IsInRange(IPAddress.Parse("127.255.255.0")));
            Assert.IsTrue(result.IsInRange(IPAddress.Parse("127.255.255.255")));

            Assert.IsFalse(result.IsInRange(IPAddress.Parse("0.0.0.0")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("126.0.0.0")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("128.0.0.0")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("255.255.255.255")));

            Assert.IsFalse(result.IsInRange(IPAddress.Parse("::1")));
        }


        /// <summary>
        ///A test for TryParse
        ///</summary>
        [TestMethod()]
        public void TryParseTest4()
        {
            AddressFamily addressFamily = AddressFamily.InterNetworkV6;
            string value = "::1/128";
            IPRange result = null;
            bool expected = true;
            bool actual;

            actual = IPRange.TryParse(addressFamily, value, out result);
            Assert.AreEqual(expected, actual);

            Assert.IsFalse(result.IsInRange(IPAddress.Parse("127.0.0.0")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("127.0.0.255")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("127.0.0.1")));

            Assert.IsTrue(result.IsInRange(IPAddress.Parse("::1")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("::2")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("::FFFF:1")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("::FFFF:1")));
        }

        /// <summary>
        ///A test for TryParse
        ///</summary>
        [TestMethod()]
        public void TryParseTest5()
        {
            AddressFamily addressFamily = AddressFamily.InterNetworkV6;
            string value = "::1/120";
            IPRange result = null;
            bool expected = true;
            bool actual;

            actual = IPRange.TryParse(addressFamily, value, out result);
            Assert.AreEqual(expected, actual);

            Assert.IsFalse(result.IsInRange(IPAddress.Parse("127.0.0.0")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("127.0.0.255")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("127.0.0.1")));

            Assert.IsTrue(result.IsInRange(IPAddress.Parse("::1")));
            Assert.IsTrue(result.IsInRange(IPAddress.Parse("::FF")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("::FFFF")));
            Assert.IsFalse(result.IsInRange(IPAddress.Parse("::100")));
        }

        [TestMethod()]
        public void ClientElementTest1()
        {
            EPiServer.Search.IndexingService.Configuration.ClientElement ce = new EPiServer.Search.IndexingService.Configuration.ClientElement();
            Assert.IsFalse(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.IsFalse(ce.IsIPAddressAllowed(IPAddress.Parse("::1")));

            ce.IPAddress = "";
            ce.IP6Address = "";
            Assert.IsFalse(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.IsFalse(ce.IsIPAddressAllowed(IPAddress.Parse("::1")));

            ce.IPAddress = "127.0.0.1";
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.IsFalse(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));

            ce.IPAddress = "127.0.0.0/24";
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));
            Assert.IsFalse(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.1.0")));

            ce.IPAddress = "127.0.0.1,127.0.1.0";
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.IsFalse(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.1.0")));

            ce.IPAddress = "127.0.0.1,,,127.0.1.0";
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.IsFalse(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.1.0")));


            ce.IPAddress = "127.0.0.1  127.0.1.0,, ";
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.IsFalse(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.1.0")));

            ce.IPAddress = "127.0.0.0/8";
            ce.IP6Address = "::1";
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.0")));
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.1.0")));
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("::1")));

        }

        [TestMethod()]
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
            Assert.IsFalse(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            Assert.IsFalse(ce.IsIPAddressAllowed(IPAddress.Parse("::1")));

            ce.AllowLocal = true;
            Assert.IsTrue(ce.IsIPAddressAllowed(IPAddress.Parse("127.0.0.1")));
            foreach (IPAddress addr in localAddresses)
            {
                Assert.IsTrue(ce.IsIPAddressAllowed(addr));
            }
        }

        [TestMethod()]
        public void IPRangeDefinedInConfigurationTest()
        {
            Assert.IsTrue(IndexingServiceSettings.ClientElements["accesskey1"].IsIPAddressAllowed(IPAddress.Parse("10.1.74.1")));
        }

    }
}
