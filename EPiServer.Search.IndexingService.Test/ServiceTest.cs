using System;
using System.Web;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using EPiServer.Search.Queries.Lucene;
using EPiServer.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Collections.ObjectModel;
using System.ServiceModel.Web;
using System.ServiceModel.Description;
using EPiServer.Data.Dynamic;
using System.Text;
using System.Linq;
using EPiServer.Search.Queries;
using EPiServer.Framework.Initialization;
using EPiServer.Framework;
using EPiServer.Data;
using EPiServer.ServiceLocation;

namespace EPiServer.Search.IndexingService.Test
{
    /// <summary>
    /// Summary description for SearchHandlerTest
    /// </summary>
    [TestClass]
    public class ServiceTest
    {
        public ServiceTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

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

        private void FlushRequestQueue()
        {
            RequestQueueHandler.ProcessQueue();
        }

        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            InitializationEngine engine = new InitializationEngine();
            engine.Modules = new IInitializableModule[] { new DataInitialization(), new SearchInitialization(), new ServiceContainerInitialization() };
            engine.Initialize(HostType.TestFramework);
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            RequestQueueHandler.TruncateQueue();
            //ResetAllIndexes();
        }

        //#region Additional test attributes
        ////
        //// You can use the following additional attributes as you write your tests:
        ////
        //// Use ClassInitialize to run code before running the first test in the class
        //// [ClassInitialize()]
        //// public static void MyClassInitialize(TestContext testContext) { }
        ////
        //// Use ClassCleanup to run code after all tests in a class have run
        //// [ClassCleanup()]
        //// public static void MyClassCleanup() { }
        ////
        //// Use TestInitialize to run code before running each test 
        //// [TestInitialize()]
        //// public void MyTestInitialize() { }
        ////
        //// Use TestCleanup to run code after each test has run
        //// [TestCleanup()]
        //// public void MyTestCleanup() { }
        ////
        //#endregion

        [TestMethod]
        public void SH_DisplayTextMaxLengthTest()
        {
            string id1 = "1";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                RequestQueueHandler.TruncateQueue();

                string s = "All work and no play makes jack a dull boy "; //43 chars
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 100; i++)
                {
                    sb.Append(s);
                }

                sb.Append("This shold be searchable metadata");


                //Add an item
                IndexRequestItem item = new IndexRequestItem(id1, IndexAction.Add);
                item.Title = "Header test";
                item.DisplayText = sb.ToString();

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                FieldQuery fq = new FieldQuery("\"searchable metadata\"");
                SearchResults res = SearchHandler.Instance.GetSearchResults(fq, 1, 10);
                Assert.AreEqual(1, res.TotalHits);
                Assert.AreEqual(500, res.IndexResponseItems[0].DisplayText.Length);
                Assert.IsFalse(res.IndexResponseItems[0].DisplayText.Contains("searchable metadata"));

                RequestQueueHandler.TruncateQueue();

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_RequestResponseItemEqualityTest()
        {
            string id1 = "1";
            string id2 = "2";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                //Add an item
                IndexRequestItem item = new IndexRequestItem(id1, IndexAction.Add);
                item.Title = "Header test";
                item.DisplayText = "Body test";
                item.Created = DateTime.Now;
                item.Modified = DateTime.Now;
                item.Uri = new Uri("http://www.google.com");
                item.Culture = "sv-SE";
                item.Authors.Add("me");
                item.Authors.Add("my self");
                item.Metadata = "Detta är ju massa meta data som man kan hålla på med";
                item.Categories.Add("cat1");
                item.Categories.Add("cat2");
                item.AccessControlList.Add("group1");
                item.AccessControlList.Add("group2");
                item.VirtualPathNodes.Add("vp1");
                item.VirtualPathNodes.Add("vp2");

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                AssertEqualToSearchResult(item, null);

                // Add item to another index with same data
                item = new IndexRequestItem(id1, IndexAction.Add);
                item.Title = "Header test";
                item.DisplayText = "Body test";
                item.Created = DateTime.Now;
                item.Modified = DateTime.Now;
                item.Uri = new Uri("http://www.google.com");
                item.Culture = "sv";
                item.Authors.Add("me");
                item.Authors.Add("my self");
                item.Metadata = "Detta är ju massa meta data som man kan hålla på med";
                item.Categories.Add("cat1");
                item.Categories.Add("cat2");
                item.AccessControlList.Add("group1");
                item.AccessControlList.Add("group2");
                item.VirtualPathNodes.Add("vp1");
                item.VirtualPathNodes.Add("vp2");

                item.NamedIndex = "testindex2";

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                AssertEqualToSearchResult(item, "testindex2");

                // Add item with no data to default index
                item = new IndexRequestItem(id2, IndexAction.Add);
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                AssertEqualToSearchResult(item, null);

                //Add an item other index with no data
                item = new IndexRequestItem(id2, IndexAction.Add);
                item.NamedIndex = "testindex2";
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                AssertEqualToSearchResult(item, "testindex2");

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_AllFieldMatchTest()
        {
            string id1 = "1";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                //Add an item
                IndexRequestItem item = new IndexRequestItem(id1, IndexAction.Add);
                item.Title = "Header test";
                item.DisplayText = "Body test";
                item.Created = DateTime.Now;
                item.Modified = DateTime.Now;
                item.Uri = new Uri("http://www.google.com");
                item.Culture = "sv";
                item.Metadata = "Detta är ju massa meta data som man kan hålla på med";
                item.ItemType = "EPiServer.Search.IndexItem, EPiServer.Search";

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                FieldQuery q1 = new FieldQuery("Header test", Field.Title);
                FieldQuery q2 = new FieldQuery("Body test", Field.DisplayText);
                FieldQuery q3 = new FieldQuery("sv", Field.Culture);
                FieldQuery q4 = new FieldQuery("EPiServer.Search.IndexItem, EPiServer.Search", Field.ItemType);

                GroupQuery gq = new GroupQuery(LuceneOperator.AND);
                gq.QueryExpressions.Add(q1);
                gq.QueryExpressions.Add(q2);
                gq.QueryExpressions.Add(q3);
                gq.QueryExpressions.Add(q4);

                SearchResults res = SearchHandler.Instance.GetSearchResults(gq, 1, 10);
                Assert.AreEqual(1, res.TotalHits);

                gq = new GroupQuery(LuceneOperator.OR);
                gq.QueryExpressions.Add(q1);
                gq.QueryExpressions.Add(q2);
                gq.QueryExpressions.Add(q3);
                gq.QueryExpressions.Add(q4);

                res = SearchHandler.Instance.GetSearchResults(gq, 1, 10);
                Assert.AreEqual(1, res.TotalHits);

                AssertEqualToSearchResult(item, null);

                //Add an item to another index
                item = new IndexRequestItem(id1, IndexAction.Add);
                item.NamedIndex = "testindex2";
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                AssertEqualToSearchResult(item, "testindex2");

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_RangeSearchTest()
        {
            string id1 = "1";
            string id2 = "2";
            string id3 = "3";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                IndexRequestItem item1 = new IndexRequestItem(id1, IndexAction.Add);
                item1.Title = "Header test";
                item1.DisplayText = "Body test";
                item1.Created = new DateTime(2010, 2, 3);
                item1.Modified = new DateTime(2010, 4, 2);
                SearchHandler.Instance.UpdateIndex(item1);

                IndexRequestItem item2 = new IndexRequestItem(id2, IndexAction.Add);
                item2.Title = "Header test";
                item2.DisplayText = "Body test";
                item2.Created = new DateTime(2009, 7, 18);
                item2.Modified = new DateTime(2009, 7, 8);
                SearchHandler.Instance.UpdateIndex(item2);

                IndexRequestItem item3 = new IndexRequestItem(id3, IndexAction.Add);
                item3.Title = "Header test";
                item3.DisplayText = "Body test";
                item3.Created = new DateTime(2009, 7, 18);
                item3.Modified = new DateTime(2009, 7, 8);
                SearchHandler.Instance.UpdateIndex(item3);

                FlushRequestQueue();

                RangeQuery r1 = new RangeQuery("20100101000000", "20100601000000", Field.Created, false);
                SearchResults res = SearchHandler.Instance.GetSearchResults(r1, 1, 10);
                Assert.AreEqual(1, res.TotalHits);

                r1 = new RangeQuery("20090101000000", "20100601000000", Field.Created, false);
                res = SearchHandler.Instance.GetSearchResults(r1, 1, 10);
                Assert.AreEqual(3, res.TotalHits);

                r1 = new RangeQuery("20100203000000", "20100402000000", Field.Created, true);
                res = SearchHandler.Instance.GetSearchResults(r1, 1, 10);
                Assert.AreEqual(1, res.TotalHits);

                r1 = new RangeQuery("20100203000000", "20100402000000", Field.Created, false);
                res = SearchHandler.Instance.GetSearchResults(r1, 1, 10);
                Assert.AreEqual(0, res.TotalHits);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_ReferenceDataSearchTest()
        {
            string id1 = "1";
            string id2 = "ref1";
            string id3 = "2";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                // Add items
                IndexRequestItem item = new IndexRequestItem(id1, IndexAction.Add);
                item.Title = "This is the header for id1 in default index";
                item.DisplayText = "This is the body for id1 in default index";
                item.Created = DateTime.Now.AddDays(-2);
                item.Metadata = "This is metadata for id1";
                SearchHandler.Instance.UpdateIndex(item);

                IndexRequestItem item2 = new IndexRequestItem(id3, IndexAction.Add);
                item2.Title = "This is the header for id2 in default index";
                item2.DisplayText = "This is the body for id2 in default index";
                item2.Created = DateTime.Now.AddDays(-2);
                item2.Metadata = "This is metadata for id2";
                SearchHandler.Instance.UpdateIndex(item2);

                FlushRequestQueue();

                // Search default field
                FieldQuery expr1 = new FieldQuery("\"header for id1\"");
                FieldQuery expr2 = new FieldQuery("\"body for id1\"");
                FieldQuery expr3 = new FieldQuery("\"metadata for id1\"");
                GroupQuery gq = new GroupQuery(LuceneOperator.AND);
                gq.QueryExpressions.Add(expr1);
                gq.QueryExpressions.Add(expr2);
                gq.QueryExpressions.Add(expr3);
                SearchResults results = SearchHandler.Instance.GetSearchResults(gq, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                AssertIndexItemEquality(item, results.IndexResponseItems[0]);

                // Search default field with not comitted reference data No hits
                expr1 = new FieldQuery("\"header for id1\"");
                expr2 = new FieldQuery("\"body for id1\"");
                expr3 = new FieldQuery("\"metadata for id1\"");
                FieldQuery expr5 = new FieldQuery("\"metadata for refItem\"");
                gq = new GroupQuery(LuceneOperator.AND);
                gq.QueryExpressions.Add(expr1);
                gq.QueryExpressions.Add(expr2);
                gq.QueryExpressions.Add(expr3);
                gq.QueryExpressions.Add(expr5);
                results = SearchHandler.Instance.GetSearchResults(gq, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count);

                // Add reference data to item
                IndexRequestItem refItem = new IndexRequestItem(id2, IndexAction.Add);
                refItem.Title = "This is the header for refItem in default index";
                refItem.DisplayText = "This is the body for refItem in default index";
                refItem.Created = DateTime.Now.AddDays(-1);
                refItem.Metadata = "This is metadata for refItem";
                refItem.ReferenceId = id1;
                SearchHandler.Instance.UpdateIndex(refItem);

                FlushRequestQueue();

                Thread.Sleep(2000); // let reference data be merged in service

                // Search for reference data
                expr1 = new FieldQuery("\"header for id1\"");
                expr2 = new FieldQuery("\"body for id1\"");
                expr3 = new FieldQuery("\"metadata for id1\"");
                expr5 = new FieldQuery("\"metadata for refItem\"");
                FieldQuery expr6 = new FieldQuery("\"header for refItem\"");
                FieldQuery expr7 = new FieldQuery("\"body for refItem\"");
                FieldQuery expr8 = new FieldQuery("\"metadata for refItem\"");
                gq = new GroupQuery(LuceneOperator.AND);
                gq.QueryExpressions.Add(expr1);
                gq.QueryExpressions.Add(expr2);
                gq.QueryExpressions.Add(expr3);
                gq.QueryExpressions.Add(expr5);
                gq.QueryExpressions.Add(expr6);
                gq.QueryExpressions.Add(expr7);
                gq.QueryExpressions.Add(expr8);
                results = SearchHandler.Instance.GetSearchResults(gq, 1, 20);

                Assert.AreEqual(1, results.IndexResponseItems.Count);
                Assert.AreEqual("1", results.IndexResponseItems[0].Id);

                AssertIndexItemEquality(item, results.IndexResponseItems[0]);

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_DataUriUpdateTest()
        {
            string id0 = "id0";
            //string id1 = "id1";
            //string id2 = "id2";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
            {
                appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            }

            string testDocumentPath0 = Path.Combine(appPath, "TestFile.txt");
            string testDocumentPath1 = Path.Combine(appPath, "test.pdf");
            string testDocumentPath2 = Path.Combine(appPath, "test.doc");
            string testDocumentPath3 = Path.Combine(appPath, "test.docx");
            string testDocumentPath4 = Path.Combine(appPath, "test big.pdf");


            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();

                //Add an item with text file
                IndexRequestItem item = new IndexRequestItem(id0, IndexAction.Add);
                item.DataUri = new Uri(testDocumentPath0);

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                Thread.Sleep(7000); //Wait for task queue in indexing service to finish

                FieldQuery fe = new FieldQuery("\"simple text file\"");
                SearchResults results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
                IndexResponseItem resultItem = results.IndexResponseItems[0];

                // Update the item with another data uri

                //Add an item with text file
                item = new IndexRequestItem(id0, IndexAction.Update);
                item.DataUri = new Uri(testDocumentPath1);

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                Thread.Sleep(5000); //Wait for task queue in indexing service to finish

                fe = new FieldQuery("\"plain and simple text file that we try to index\"");
                results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");

                fe = new FieldQuery("\"test pdf document\"");
                results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");

                // Update the item with no data uri
                item = new IndexRequestItem(id0, IndexAction.Update);
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                Thread.Sleep(5000); //Wait for task queue in indexing service to finish

                fe = new FieldQuery("\"test pdf document\"");
                results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_DataUriAddPDFTest()
        {
            string id0 = "id0";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
            {
                appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            }

            string testDocumentPath0 = Path.Combine(appPath, "test.pdf");

            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();

                //Add an item with text file
                IndexRequestItem item = new IndexRequestItem(id0, IndexAction.Add);
                item.Title = "Text Header test";
                item.DisplayText = "Text Body test";
                item.Created = DateTime.Now;
                item.Modified = DateTime.Now;
                item.Uri = new Uri("http://www.google.com");
                item.Culture = "sv";
                item.Metadata = "This is meta data";
                item.DataUri = new Uri(testDocumentPath0);

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                Thread.Sleep(5000); //Wait for task queue in indexing service to finish

                FieldQuery fe = new FieldQuery("\"test pdf document\"");
                SearchResults results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_DataUriAddBigPDFTest()
        {
            string id0 = "id0";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
            {
                appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            }

            string testDocumentPath0 = Path.Combine(appPath, "test big.pdf");

            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();

                //Add an item with text file
                IndexRequestItem item = new IndexRequestItem(id0, IndexAction.Add);
                item.Title = "Text Header test";
                item.DisplayText = "Text Body test";
                item.Created = DateTime.Now;
                item.Modified = DateTime.Now;
                item.Uri = new Uri("http://www.google.com");
                item.Culture = "sv";
                item.Metadata = "This is meta data";
                item.DataUri = new Uri(testDocumentPath0);

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                Thread.Sleep(20000); //Wait for task queue in indexing service to finish

                FieldQuery fe = new FieldQuery("\"StarCommunity is actually a module of Required Framework Components\"");
                SearchResults results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_DataUriAddWordDocXTest()
        {
            string id0 = "id0";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
            {
                appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            }

            string testDocumentPath0 = Path.Combine(appPath, "test.docx");

            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();

                //Add an item with text file
                IndexRequestItem item = new IndexRequestItem(id0, IndexAction.Add);
                item.Title = "Text Header test";
                item.DisplayText = "Text Body test";
                item.Created = DateTime.Now;
                item.Modified = DateTime.Now;
                item.Uri = new Uri("http://www.google.com");
                item.Culture = "sv";
                item.Metadata = "This is meta data";
                item.DataUri = new Uri(testDocumentPath0);

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                Thread.Sleep(5000); //Wait for task queue in indexing service to finish

                FieldQuery fe = new FieldQuery("\"This is a test word document\"");
                SearchResults results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_DataUriAddWordDocTest()
        {
            string id0 = "id0";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
            {
                appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            }

            string testDocumentPath0 = Path.Combine(appPath, "test.doc");

            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();

                //Add an item with text file
                IndexRequestItem item = new IndexRequestItem(id0, IndexAction.Add);
                item.Title = "Text Header test";
                item.DisplayText = "Text Body test";
                item.Created = DateTime.Now;
                item.Modified = DateTime.Now;
                item.Uri = new Uri("http://www.google.com");
                item.Culture = "sv";
                item.Metadata = "This is meta data";
                item.DataUri = new Uri(testDocumentPath0);

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                Thread.Sleep(5000); //Wait for task queue in indexing service to finish

                FieldQuery fe = new FieldQuery("\"test word 2007\"");
                SearchResults results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_DataUriAddTextFileTest()
        {
            string id0 = "id0";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
            {
                appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            }

            string testDocumentPath0 = Path.Combine(appPath, "TestFile.txt");

            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();

                //Add an item with text file
                IndexRequestItem item = new IndexRequestItem(id0, IndexAction.Add);
                item.Title = "Text Header test";
                item.DisplayText = "Text Body test";
                item.Created = DateTime.Now;
                item.Modified = DateTime.Now;
                item.Uri = new Uri("http://www.google.com");
                item.Culture = "sv";
                item.Metadata = "This is meta data";
                item.DataUri = new Uri(testDocumentPath0);

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                Thread.Sleep(5000); //Wait for task queue in indexing service to finish

                FieldQuery fe = new FieldQuery("\"plain and simple text file that we try to index\"");
                SearchResults results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
                IndexResponseItem resultItem = results.IndexResponseItems[0];
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_DataUriWithReferenceTest()
        {
            string id0 = "id0";
            string id1 = "id1";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
            {
                appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            }

            string testDocumentPath0 = Path.Combine(appPath, "test.pdf");

            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();

                //Add an item with a pdf file
                IndexRequestItem item1 = new IndexRequestItem(id0, IndexAction.Add);
                item1.Title = "Text Header test";
                item1.DisplayText = "Text Body test";
                item1.DataUri = new Uri(testDocumentPath0);

                SearchHandler.Instance.UpdateIndex(item1);

                //Add an item referencing id1
                IndexRequestItem item2 = new IndexRequestItem(id1, IndexAction.Add);
                item2.Title = "Text Header test reference item";
                item2.DisplayText = "Text Body test";
                item2.ReferenceId = id0;

                SearchHandler.Instance.UpdateIndex(item2);

                FlushRequestQueue();

                Thread.Sleep(5000); //Wait for task queue in indexing service to finish

                FieldQuery fe = new FieldQuery("\"test pdf document\"");
                SearchResults results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
                Assert.AreEqual(id0, results.IndexResponseItems[0].Id);

                fe = new FieldQuery("\"test reference item\"");
                results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
                Assert.AreEqual(id0, results.IndexResponseItems[0].Id);

                // Update reference item
                IndexRequestItem item3 = new IndexRequestItem(id1, IndexAction.Update);
                item3.Title = "Text Header test reference item update";
                item3.DisplayText = "Text Body test";
                item3.ReferenceId = id0;

                SearchHandler.Instance.UpdateIndex(item3);

                FlushRequestQueue();

                fe = new FieldQuery("\"test reference item update\"");
                results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
                Assert.AreEqual(id0, results.IndexResponseItems[0].Id);

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_DataUriAsReferenceTest()
        {
            string id0 = "id0";
            string id1 = "id1";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
            {
                appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            }

            string testDocumentPath0 = Path.Combine(appPath, "test.pdf");

            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();

                //Add an item
                IndexRequestItem item1 = new IndexRequestItem(id0, IndexAction.Add);
                item1.Title = "Text title main item";
                item1.DisplayText = "Text Body test";

                SearchHandler.Instance.UpdateIndex(item1);

                //Add an item with a pdf file uri referencing id0
                IndexRequestItem item2 = new IndexRequestItem(id1, IndexAction.Add);
                item2.Title = "Text title test";
                item2.DisplayText = "Text Body test";
                item2.DataUri = new Uri(testDocumentPath0);
                item2.ReferenceId = id0; 

                SearchHandler.Instance.UpdateIndex(item2);
                
                FlushRequestQueue();

                Thread.Sleep(5000); //Wait for task queue in indexing service to finish

                FieldQuery fe = new FieldQuery("\"title main item\"");
                SearchResults results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
                Assert.AreEqual(id0, results.IndexResponseItems[0].Id);

                fe = new FieldQuery("\"test pdf document\"");
                results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
                Assert.AreEqual(id0, results.IndexResponseItems[0].Id);

                // Update reference item
                IndexRequestItem item3 = new IndexRequestItem(id1, IndexAction.Update);
                item3.Title = "Text title test reference item update";
                item3.DisplayText = "Text Body test";
                item3.DataUri = new Uri(testDocumentPath0);
                item3.ReferenceId = id0;

                SearchHandler.Instance.UpdateIndex(item3);

                FlushRequestQueue();

                Thread.Sleep(5000); //Wait for task queue in indexing service to finish

                fe = new FieldQuery("\"test reference item update\"");
                results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
                Assert.AreEqual(id0, results.IndexResponseItems[0].Id);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_ACLTest()
        {
            string id1 = "1";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {

                //Reset indexes
                ResetAllIndexes();

                //Add an item
                IndexRequestItem item = new IndexRequestItem(id1, IndexAction.Add);
                item.Title = "Header test";
                item.DisplayText = "Body test";
                item.Created = DateTime.Now;
                item.Modified = DateTime.Now;
                item.Uri = new Uri("http://www.google.com");
                item.Culture = "sv";
                item.Metadata = "Detta är ju massa meta data som man kan hålla på med";
                item.AccessControlList.Add("G:me");
                item.AccessControlList.Add("U:myself");
                item.AccessControlList.Add("G:and irene");

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                FieldQuery fe = new FieldQuery(item.Id, Field.Id);

                AccessControlListQuery aclQuery = new AccessControlListQuery();
                aclQuery.Items.Add("G:me");

                GroupQuery gq1 = new GroupQuery(LuceneOperator.AND);
                gq1.QueryExpressions.Add(fe);
                gq1.QueryExpressions.Add(aclQuery);

                SearchResults results = SearchHandler.Instance.GetSearchResults(gq1, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
                IndexResponseItem resultItem = results.IndexResponseItems[0];

                AssertIndexItemEquality(item, resultItem);

                //Group test
                FieldQuery fe2 = new FieldQuery(item.Id, Field.Id);
                AccessControlListQuery aclQuery2 = new AccessControlListQuery();
                aclQuery2.Items.Add("G:and irene");

                GroupQuery gq2 = new GroupQuery(LuceneOperator.AND);
                gq2.QueryExpressions.Add(fe2);
                gq2.QueryExpressions.Add(aclQuery2);

                results = SearchHandler.Instance.GetSearchResults(gq2, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
                resultItem = results.IndexResponseItems[0];
                AssertIndexItemEquality(item, resultItem);

                //Groups and users test
                FieldQuery fe3 = new FieldQuery(item.Id, Field.Id);
                AccessControlListQuery aclQuery3 = new AccessControlListQuery();
                aclQuery3.Items.Add("G:me");
                aclQuery3.Items.Add("G:and irene");
                aclQuery3.Items.Add("U:myself");

                GroupQuery gq3 = new GroupQuery(LuceneOperator.AND);
                gq3.QueryExpressions.Add(fe3);
                gq3.QueryExpressions.Add(aclQuery3);

                results = SearchHandler.Instance.GetSearchResults(gq3, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
                resultItem = results.IndexResponseItems[0];

                AssertEqualToSearchResult(item, null);

                //No access test
                FieldQuery fe4 = new FieldQuery(item.Id, Field.Id);
                AccessControlListQuery aclQuery4 = new AccessControlListQuery();
                aclQuery4.Items.Add("G:no access");

                GroupQuery gq4 = new GroupQuery(LuceneOperator.AND);
                gq4.QueryExpressions.Add(fe4);
                gq4.QueryExpressions.Add(aclQuery4);

                results = SearchHandler.Instance.GetSearchResults(gq4, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");

                //Update the item
                item = new IndexRequestItem(id1, IndexAction.Update);
                item.Title = "Header test";
                item.DisplayText = "Body test";
                item.Created = DateTime.Now;
                item.Modified = DateTime.Now;
                item.Uri = new Uri("http://www.google.com");
                item.Culture = "sv";
                item.Metadata = "Detta är ju massa meta data som man kan hålla på med";
                item.AccessControlList.Add("G:me");
                item.AccessControlList.Add("U:myself");

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                // Check that "and irene" dont have access after update
                AccessControlListQuery aclQuery5 = new AccessControlListQuery();
                aclQuery5.Items.Add("G:and irene");
                FieldQuery fe5 = new FieldQuery(item.Id, Field.Id);
                GroupQuery gq5 = new GroupQuery(LuceneOperator.AND);
                gq5.QueryExpressions.Add(fe5);
                gq5.QueryExpressions.Add(aclQuery5);
                results = SearchHandler.Instance.GetSearchResults(gq2, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");

                // Check inner operator "AND"
                AccessControlListQuery aclQuery6 = new AccessControlListQuery(LuceneOperator.AND);
                aclQuery6.Items.Add("G:me");
                aclQuery6.Items.Add("U:myself");
                results = SearchHandler.Instance.GetSearchResults(aclQuery6, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_RemoveTest()
        {
            string id = Guid.NewGuid().ToString();
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();


            try
            {
                //Reset indexes
                ResetAllIndexes();

                //Add two items
                IndexRequestItem item1 = new IndexRequestItem(id, IndexAction.Add);
                SearchHandler.Instance.UpdateIndex(item1);

                IndexRequestItem item2 = new IndexRequestItem(id, IndexAction.Add);
                item2.NamedIndex = "testindex2";
                SearchHandler.Instance.UpdateIndex(item2);

                FlushRequestQueue();

                AssertEqualToSearchResult(item1, null);
                AssertEqualToSearchResult(item2, "testindex2");

                //Remove item from default index
                IndexRequestItem item3 = new IndexRequestItem(id, IndexAction.Remove);
                SearchHandler.Instance.UpdateIndex(item3);

                FlushRequestQueue();

                //The item should be removed
                EscapedFieldQuery fe = new EscapedFieldQuery(id, Field.Id);
                SearchResults results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count);


                //Remove item from named index index
                IndexRequestItem item4 = new IndexRequestItem(id, IndexAction.Remove);
                item4.NamedIndex = "testindex2";
                SearchHandler.Instance.UpdateIndex(item4);

                FlushRequestQueue();

                //The item should be removed
                Collection<string> namedIndexes = new Collection<string>();
                fe = new EscapedFieldQuery(id, Field.Id);
                results = SearchHandler.Instance.GetSearchResults(fe, null, namedIndexes, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_ReservedCharactersInIdTest()
        {
            string id = "~" + Guid.NewGuid().ToString() + "(";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();


            try
            {
                //Reset indexes
                ResetAllIndexes();

                //Add two items
                IndexRequestItem item1 = new IndexRequestItem(id, IndexAction.Add);
                SearchHandler.Instance.UpdateIndex(item1);

                IndexRequestItem item2 = new IndexRequestItem(id, IndexAction.Add);
                item2.NamedIndex = "testindex2";
                SearchHandler.Instance.UpdateIndex(item2);

                FlushRequestQueue();

                AssertEqualToSearchResult(item1, null);
                AssertEqualToSearchResult(item2, "testindex2");

                //Remove item from default index
                IndexRequestItem item3 = new IndexRequestItem(id, IndexAction.Remove);
                SearchHandler.Instance.UpdateIndex(item3);

                FlushRequestQueue();

                //The item should be removed
                EscapedFieldQuery fe = new EscapedFieldQuery(id, Field.Id);
                SearchResults results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count);


                //Remove item from named index index
                IndexRequestItem item4 = new IndexRequestItem(id, IndexAction.Remove);
                item4.NamedIndex = "testindex2";
                SearchHandler.Instance.UpdateIndex(item4);

                FlushRequestQueue();

                //The item should be removed
                Collection<string> namedIndexes = new Collection<string>();
                fe = new EscapedFieldQuery(id, Field.Id);
                results = SearchHandler.Instance.GetSearchResults(fe, null, namedIndexes, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_UpdateTest()
        {
            string id = Guid.NewGuid().ToString();
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                //Add an item
                IndexRequestItem item1 = new IndexRequestItem(id, IndexAction.Add);
                item1.Title = "header test";
                SearchHandler.Instance.UpdateIndex(item1);

                FlushRequestQueue();

                AssertEqualToSearchResult(item1, null);

                //Update the item
                IndexRequestItem item2 = new IndexRequestItem(id, IndexAction.Update);
                item2.Title = "header test updated";
                SearchHandler.Instance.UpdateIndex(item2);

                FlushRequestQueue();

                AssertEqualToSearchResult(item2, null);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_UpdateNamedIndexTest()
        {
            string id = Guid.NewGuid().ToString();
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                //Add an item
                IndexRequestItem item1 = new IndexRequestItem(id, IndexAction.Add);
                item1.Title = "header test";
                item1.DisplayText = "body test";
                item1.NamedIndex = "testindex3";
                SearchHandler.Instance.UpdateIndex(item1);

                FlushRequestQueue();

                AssertEqualToSearchResult(item1, "testindex3");

                //Update the item by including the update endpoint for the mockup service
                IndexRequestItem item2 = new IndexRequestItem(id, IndexAction.Update);
                item2.Title = "header test updated";
                item2.DisplayText = "body test updated";
                item2.NamedIndex = "testindex3";
                SearchHandler.Instance.UpdateIndex(item2);

                FlushRequestQueue();

                AssertEqualToSearchResult(item2, "testindex3");

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_PagingTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            SearchSettings.Config.UseIndexingServicePaging = true;

            try
            {
                //Reset indexes
                ResetAllIndexes();

                CreateMultipleRequests();

                FlushRequestQueue();

                SearchResults results = null;

                //Get all items where any of the words exist in field: default and index: default
                FieldQuery expr = new FieldQuery("header");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 7);
                Assert.AreEqual(7, results.TotalHits);
                Assert.AreEqual(7, results.IndexResponseItems.Count);

                expr = new FieldQuery("header");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 3);
                Assert.AreEqual(7, results.TotalHits);
                Assert.AreEqual(3, results.IndexResponseItems.Count);

                expr = new FieldQuery("header");
                results = SearchHandler.Instance.GetSearchResults(expr, 2, 3);
                Assert.AreEqual(7, results.TotalHits);
                Assert.AreEqual(3, results.IndexResponseItems.Count);

                expr = new FieldQuery("header");
                results = SearchHandler.Instance.GetSearchResults(expr, 3, 3);
                Assert.AreEqual(7, results.TotalHits);
                Assert.AreEqual(1, results.IndexResponseItems.Count);
            }
            finally
            {

                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_DefaultFieldTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();
            try
            {
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem("1", IndexAction.Add);
                item.Title = "testar lite svårt";
                item.DisplayText = "testing introtext";
                SearchHandler.Instance.UpdateIndex(item);

                item = new IndexRequestItem("2", IndexAction.Add);
                item.Title = "testing header2";
                item.DisplayText = "testar lite svårt";
                SearchHandler.Instance.UpdateIndex(item);

                item = new IndexRequestItem("3", IndexAction.Add);
                item.Title = "header3";
                item.DisplayText = "testar lite lätt";
                item.Metadata = "metadata med svårt innehåll";
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                FieldQuery q = new FieldQuery("testing");
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(2, res.IndexResponseItems.Count);

                q = new FieldQuery("svårt");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(3, res.IndexResponseItems.Count);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_UpdateNonExistingItemShouldAdd()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();
            try
            {
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem("1", IndexAction.Update);
                item.Title = "testar lite svårt";
                item.DisplayText = "testing introtext";
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                FieldQuery q = new FieldQuery("testing");
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
            }
            finally
            {
                sh1.Close();
            }
        }


        [TestMethod]
        public void SH_FieldExpressionsTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();

                CreateMultipleRequests();
                FlushRequestQueue();

                SearchResults results = null;

                //Get all items where any of the words exist in field: default and index: default
                FieldQuery expr = new FieldQuery("\"header for\"");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(7, results.IndexResponseItems.Count);

                GroupQuery gq = new GroupQuery(LuceneOperator.AND);
                FieldQuery fq1 = new FieldQuery("\"this is\"");
                FieldQuery fq2 = new FieldQuery("\"header for id3\"");
                gq.QueryExpressions.Add(fq1);
                gq.QueryExpressions.Add(fq2);
                results = SearchHandler.Instance.GetSearchResults(gq, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                gq = new GroupQuery(LuceneOperator.OR);
                fq1 = new FieldQuery("\"this is\"");
                fq2 = new FieldQuery("\"header for id3\"");
                gq.QueryExpressions.Add(fq1);
                gq.QueryExpressions.Add(fq2);
                results = SearchHandler.Instance.GetSearchResults(gq, 1, 20);
                Assert.AreEqual(7, results.IndexResponseItems.Count);

                expr = new FieldQuery("\"är data i body\"");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                expr = new FieldQuery("\"är Data i Body\"");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                expr = new FieldQuery("\"är Data i Body*\"");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                expr = new FieldQuery("\"är data i meta\"");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                expr = new FieldQuery("\"testas lite svårt\"");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                expr = new FieldQuery("\"testas lite svart\"");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count);

                expr = new FieldQuery("svårt");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                //Get top 5 items where any of the words exist in field: default and index: default
                expr = new FieldQuery("\"header for\"");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 5);
                Assert.AreEqual(5, results.IndexResponseItems.Count);

                //Get all items where the exact phrase exist in field: default and index: default
                expr = new FieldQuery("\"header for id1\"");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                //Get all items where the exact phrase exist in field: header and index: default
                expr = new FieldQuery("\"header for id1\"", Field.Title);
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                //Get all items where the exact phrase exist in field: body and index: default
                expr = new FieldQuery("\"header for\"", Field.DisplayText);
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count);

                //Get all items where any of the words exist in field: header and index: testindex2
                expr = new FieldQuery("\"header for\"", Field.Title);
                Collection<string> indexes1 = new Collection<string>();
                indexes1.Add("testindex2");
                results = SearchHandler.Instance.GetSearchResults(expr, null, indexes1, 1, 20);
                Assert.AreEqual(3, results.IndexResponseItems.Count);

                //Get all items where any of the words exist in field: default and index: testindex3
                expr = new FieldQuery("\"header for\"");
                Collection<string> indexes2 = new Collection<string>();
                indexes2.Add("testindex3");
                results = SearchHandler.Instance.GetSearchResults(expr, null, indexes2, 1, 20);
                Assert.AreEqual(4, results.IndexResponseItems.Count);

                expr = new FieldQuery("\"header for\"");
                Collection<string> indexes3 = new Collection<string>();
                indexes3.Add("testindex2");
                indexes3.Add("testindex3");
                results = SearchHandler.Instance.GetSearchResults(expr, null, indexes3, 1, 20);
                Assert.AreEqual(7, results.IndexResponseItems.Count);

                expr = new FieldQuery("Cms", Field.ItemType);
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 100);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                //expr = new FieldQuery("Cm*", Field.Type);
                //results = SearchHandler.Instance.GetSearchResults(expr, 1, 100);
                //Assert.AreEqual(1, results.IndexResponseItems.Count);

                //expr = new FieldQuery("EPiServer.Common*", Field.Type);
                //results = SearchHandler.Instance.GetSearchResults(expr, 1, 100);
                //Assert.AreEqual(1, results.IndexResponseItems.Count);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_TypeFieldSearchTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                //Add items to different indexes
                IndexRequestItem item1 = new IndexRequestItem(id1, IndexAction.Add);
                item1.ItemType = "CmsPage";
                SearchHandler.Instance.UpdateIndex(item1);

                IndexRequestItem item2 = new IndexRequestItem(id2, IndexAction.Add);
                item2.ItemType = "EPiServer.Common.Comment, EPiServer.Common";
                SearchHandler.Instance.UpdateIndex(item2);

                IndexRequestItem item3 = new IndexRequestItem(id3, IndexAction.Add);
                item3.ItemType = "Car.carpool";
                SearchHandler.Instance.UpdateIndex(item3);

                FlushRequestQueue();

                SearchResults results = null;

                FieldQuery expr = new FieldQuery("CmsPage", Field.ItemType);
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                expr = new FieldQuery("Car.car*", Field.ItemType);
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                expr = new FieldQuery("EPiServer.Common*", Field.ItemType);
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                expr = new FieldQuery("EPiServer.Common", Field.ItemType);
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);
            }
            finally
            {

                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_FuzzySearchTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                //Add items to different indexes
                IndexRequestItem item1 = new IndexRequestItem(id1, IndexAction.Add);
                item1.DisplayText = "you may reconcider this";
                SearchHandler.Instance.UpdateIndex(item1);

                IndexRequestItem item2 = new IndexRequestItem(id2, IndexAction.Add);
                item2.DisplayText = "and you may reconcideration this as well";
                item2.NamedIndex = "testindex2";
                SearchHandler.Instance.UpdateIndex(item2);

                FlushRequestQueue();

                SearchResults results = null;

                FieldQuery expr = new FieldQuery("reconcider");
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                Collection<string> namedIndexes = new Collection<string>();
                namedIndexes.Add("default");
                namedIndexes.Add("testindex2");

                //Get all items where any of the words exist in field: default and index: default
                expr = new FuzzyQuery("reconcider", Field.Default, 0.9f);
                results = SearchHandler.Instance.GetSearchResults(expr, null, namedIndexes, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                expr = new FuzzyQuery("reconcider", Field.Default, 0.1f);
                results = SearchHandler.Instance.GetSearchResults(expr, null, namedIndexes, 1, 20);
                Assert.AreEqual(2, results.IndexResponseItems.Count);

            }
            finally
            {

                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_ProximitySearchTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                //Add items to different indexes

                IndexRequestItem item1 = new IndexRequestItem(id1, IndexAction.Add);
                item1.DisplayText = "test body for id1 in default index";
                SearchHandler.Instance.UpdateIndex(item1);

                IndexRequestItem item2 = new IndexRequestItem(id2, IndexAction.Add);
                item2.DisplayText = "test body for id2 in default index";
                SearchHandler.Instance.UpdateIndex(item2);

                IndexRequestItem item3 = new IndexRequestItem(id3, IndexAction.Add);
                item3.DisplayText = "test body for id3 in default index";
                item3.NamedIndex = "testindex2";
                SearchHandler.Instance.UpdateIndex(item3);

                FlushRequestQueue();

                //Assert.IsTrue(wh.WaitOne(10000)); //Timeout due to that the queue was never processed

                SearchResults results = null;

                FieldQuery expr = new ProximityQuery("\"body index\"", Field.Default, 1);
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count);

                expr = new ProximityQuery("\"body index\"", Field.Default, 4);
                results = SearchHandler.Instance.GetSearchResults(expr, 1, 20);
                Assert.AreEqual(2, results.IndexResponseItems.Count);

                Collection<string> namedIndexes = new Collection<string>();
                namedIndexes.Add("testindex2");

                expr = new ProximityQuery("\"body index\"", Field.Default, 4);
                results = SearchHandler.Instance.GetSearchResults(expr, null, namedIndexes, 1, 20);
                Assert.AreEqual(1, results.IndexResponseItems.Count);

                namedIndexes.Add("default");
                expr = new ProximityQuery("\"body index\"", Field.Default, 4);
                results = SearchHandler.Instance.GetSearchResults(expr, null, namedIndexes, 1, 20);
                Assert.AreEqual(3, results.IndexResponseItems.Count);
            }
            finally
            {

                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_TermBoostSearchTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                //Add items
                IndexRequestItem item1 = new IndexRequestItem(id1, IndexAction.Add);
                item1.Title = "test header for id1 in default index";
                item1.DisplayText = "test body for id1 in default index";
                SearchHandler.Instance.UpdateIndex(item1);

                IndexRequestItem item2 = new IndexRequestItem(id2, IndexAction.Add);
                item2.Title = "test header for id2 in default index";
                item2.DisplayText = "test body for id2 in default index";
                SearchHandler.Instance.UpdateIndex(item2);

                IndexRequestItem item3 = new IndexRequestItem(id3, IndexAction.Add);
                item3.Title = "test header for id3 in default index";
                item3.DisplayText = "test body for id3 in default index";
                SearchHandler.Instance.UpdateIndex(item3);

                FlushRequestQueue();

                //Assert.IsTrue(wh.WaitOne(10000)); //Timeout due to that the queue was never processed

                SearchResults results = null;

                TermBoostQuery t1 = new TermBoostQuery("\"for id2\"", 20);

                TermBoostQuery t2 = new TermBoostQuery("\"for id3\"", 3);

                FieldQuery f1 = new FieldQuery("header");

                GroupQuery g = new GroupQuery(LuceneOperator.OR);
                g.QueryExpressions.Add(t1);
                g.QueryExpressions.Add(t2);
                g.QueryExpressions.Add(f1);

                results = SearchHandler.Instance.GetSearchResults(g, 1, 20);
                Assert.AreEqual(3, results.TotalHits);
                Assert.AreEqual(id2, results.IndexResponseItems[0].Id);
                Assert.AreEqual(id3, results.IndexResponseItems[1].Id);
                Assert.AreEqual(id1, results.IndexResponseItems[2].Id);

                t1 = new TermBoostQuery("\"for id2\"", 3);

                t2 = new TermBoostQuery("\"for id3\"", 20);

                f1 = new FieldQuery("header");

                g = new GroupQuery(LuceneOperator.OR);
                g.QueryExpressions.Add(t1);
                g.QueryExpressions.Add(t2);
                g.QueryExpressions.Add(f1);

                results = SearchHandler.Instance.GetSearchResults(g, 1, 20);
                Assert.AreEqual(3, results.TotalHits);
                Assert.AreEqual(id3, results.IndexResponseItems[0].Id);
                Assert.AreEqual(id2, results.IndexResponseItems[1].Id);
                Assert.AreEqual(id1, results.IndexResponseItems[2].Id);


            }
            finally
            {
                sh1.Close();
            }
        }


        [TestMethod]
        public void SH_CategoriesSearchTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                IndexRequestItem item1 = new IndexRequestItem(id1, IndexAction.Add);
                item1.Categories.Add("tag1");
                item1.Categories.Add("tag2");
                item1.Categories.Add("tag1 tag2");
                SearchHandler.Instance.UpdateIndex(item1);

                IndexRequestItem item2 = new IndexRequestItem(id2, IndexAction.Add);
                item2.Categories.Add("entity3/cars");
                item2.Categories.Add("tag1");
                SearchHandler.Instance.UpdateIndex(item2);

                FlushRequestQueue();

                //Assert.IsTrue(wh.WaitOne(10000)); //Timeout due to that the queue was never processed

                SearchResults results = null;

                Thread.Sleep(3000);

                CategoryQuery categoriesQuery1 = new CategoryQuery(LuceneOperator.AND);
                categoriesQuery1.Items.Add("tag1");

                results = SearchHandler.Instance.GetSearchResults(categoriesQuery1, 1, 20);
                Assert.AreEqual(2, results.TotalHits);

                CategoryQuery categoriesQuery2 = new CategoryQuery(LuceneOperator.AND);
                categoriesQuery2.Items.Add("tag2");
                results = SearchHandler.Instance.GetSearchResults(categoriesQuery2, 1, 20);
                Assert.AreEqual(1, results.TotalHits);

                CategoryQuery categoriesQuery3 = new CategoryQuery(LuceneOperator.AND);
                categoriesQuery3.Items.Add("tag1 tag2");
                results = SearchHandler.Instance.GetSearchResults(categoriesQuery3, 1, 20);
                Assert.AreEqual(1, results.TotalHits);

                GroupQuery group = new GroupQuery(LuceneOperator.OR);
                group.QueryExpressions.Add(categoriesQuery1);
                results = SearchHandler.Instance.GetSearchResults(group, 1, 20);
                Assert.AreEqual(2, results.TotalHits);

                group.QueryExpressions.Add(categoriesQuery3);
                results = SearchHandler.Instance.GetSearchResults(group, 1, 20);
                Assert.AreEqual(2, results.TotalHits);
            }
            finally
            {

                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_VirtualPathSearchTest()
        {
            string id1 = Guid.NewGuid().ToString();
            string id2 = Guid.NewGuid().ToString();
            string id3 = Guid.NewGuid().ToString();
            string id4 = Guid.NewGuid().ToString();
            string id5 = Guid.NewGuid().ToString();
            string id6 = Guid.NewGuid().ToString();

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                IndexRequestItem item1 = new IndexRequestItem(id1, IndexAction.Add);
                item1.Title = "testing header";
                item1.Metadata = "testing metadata";
                item1.VirtualPathNodes.Add("Node 1");
                item1.VirtualPathNodes.Add("node 1_1");
                item1.VirtualPathNodes.Add("node 1_2");
                SearchHandler.Instance.UpdateIndex(item1);

                IndexRequestItem item2 = new IndexRequestItem(id2, IndexAction.Add);
                item2.Title = "testing header";
                item2.Metadata = "testing metadata";
                item2.VirtualPathNodes.Add("node2");
                item2.VirtualPathNodes.Add("node 1_1");
                SearchHandler.Instance.UpdateIndex(item2);

                IndexRequestItem item3 = new IndexRequestItem(id3, IndexAction.Add);
                item3.Title = "testing header";
                item3.Metadata = "testing metadata";
                item3.VirtualPathNodes.Add("Node 1");
                item3.VirtualPathNodes.Add("node 1_3");
                item3.VirtualPathNodes.Add("node 1_4");
                SearchHandler.Instance.UpdateIndex(item3);

                IndexRequestItem item4 = new IndexRequestItem(id4, IndexAction.Add);
                item4.Title = "the fourth item header4";
                item4.VirtualPathNodes.Add("Node 1");
                item4.VirtualPathNodes.Add("node 1_3");
                item4.VirtualPathNodes.Add("node 1_4");
                SearchHandler.Instance.UpdateIndex(item4);

                IndexRequestItem item5 = new IndexRequestItem(id5, IndexAction.Add);
                item5.Title = "the item header5";
                item5.VirtualPathNodes.Add("Node 1");
                item5.VirtualPathNodes.Add("node 1_3");
                item5.VirtualPathNodes.Add("node 1_5");
                SearchHandler.Instance.UpdateIndex(item5);

                string node1 = Guid.NewGuid().ToString();
                string node2 = Guid.NewGuid().ToString();

                IndexRequestItem item6 = new IndexRequestItem(id6, IndexAction.Add);
                item6.Title = "the item header6";
                item6.VirtualPathNodes.Add(node1);
                item6.VirtualPathNodes.Add(node2);
                SearchHandler.Instance.UpdateIndex(item6);

                FlushRequestQueue();

                SearchResults results = null;

                // Make sure we get 2 hits for "node 1"
                VirtualPathQuery vpq1 = new VirtualPathQuery();
                vpq1.VirtualPathNodes.Add("Node 1");
                FieldQuery fq1 = new FieldQuery("testing header");
                GroupQuery gq1 = new GroupQuery(LuceneOperator.AND);
                gq1.QueryExpressions.Add(fq1);
                gq1.QueryExpressions.Add(vpq1);

                results = SearchHandler.Instance.GetSearchResults(gq1, 1, 20);
                Assert.AreEqual(2, results.TotalHits);

                // Make sure we get 2 hits for "node 1/node 1_1"
                VirtualPathQuery vpq2 = new VirtualPathQuery();
                vpq2.VirtualPathNodes.Add("Node 1");
                vpq2.VirtualPathNodes.Add("node 1_1");
                FieldQuery fq2 = new FieldQuery("testing header");
                GroupQuery gq2 = new GroupQuery(LuceneOperator.AND);
                gq2.QueryExpressions.Add(fq2);
                gq2.QueryExpressions.Add(vpq2);

                results = SearchHandler.Instance.GetSearchResults(gq2, 1, 20);
                Assert.AreEqual(1, results.TotalHits);

                // Make sure we get 1 hit for "node 1/node 1_1/node 1_2"
                VirtualPathQuery vpq3 = new VirtualPathQuery();
                vpq3.VirtualPathNodes.Add("Node 1");
                vpq3.VirtualPathNodes.Add("node 1_1");
                vpq3.VirtualPathNodes.Add("node 1_2");
                FieldQuery fq3 = new FieldQuery("testing header");
                GroupQuery gq3 = new GroupQuery(LuceneOperator.AND);
                gq3.QueryExpressions.Add(fq3);
                gq3.QueryExpressions.Add(vpq3);

                results = SearchHandler.Instance.GetSearchResults(gq3, 1, 20);
                Assert.AreEqual(1, results.TotalHits);
                IndexItemBase resultItem = results.IndexResponseItems[0];
                AssertIndexItemEquality(item1, resultItem);

                // Make sure we get 2 hits for "node 1/node 1_3/node 1_4"
                VirtualPathQuery vpq4 = new VirtualPathQuery();
                vpq4.VirtualPathNodes.Add("Node 1");
                vpq4.VirtualPathNodes.Add("node 1_3");
                vpq4.VirtualPathNodes.Add("node 1_4");

                results = SearchHandler.Instance.GetSearchResults(vpq4, 1, 20);
                Assert.AreEqual(2, results.TotalHits);

                // Make sure we get 0 hits for "node 1_1" because its not a starting node
                VirtualPathQuery vpq5 = new VirtualPathQuery();
                vpq5.VirtualPathNodes.Add("node 1_1");

                results = SearchHandler.Instance.GetSearchResults(vpq5, 1, 20);
                Assert.AreEqual(0, results.TotalHits);

                // Make sure we get 3 hits for "node 1/node 1_3/"
                VirtualPathQuery vpq6 = new VirtualPathQuery();
                vpq6.VirtualPathNodes.Add("Node 1");
                vpq6.VirtualPathNodes.Add("node 1_3");

                results = SearchHandler.Instance.GetSearchResults(vpq6, 1, 20);
                Assert.AreEqual(3, results.TotalHits);

                // Make sure we get 1 hit for guid formatted nodes (id6)
                VirtualPathQuery vpq7 = new VirtualPathQuery();
                vpq7.VirtualPathNodes.Add(node1);

                results = SearchHandler.Instance.GetSearchResults(vpq7, 1, 20);
                Assert.AreEqual(1, results.TotalHits);

            }
            finally
            {

                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_VirtualPathUpdateTest()
        {
            string id1 = Guid.NewGuid().ToString();
            string id2 = Guid.NewGuid().ToString();
            string id3 = Guid.NewGuid().ToString();
            string id4 = Guid.NewGuid().ToString();
            string id5 = Guid.NewGuid().ToString();

            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                // id1
                IndexRequestItem item = new IndexRequestItem(id1, IndexAction.Add);
                item.Title = "testing header";
                item.DisplayText = "testing introtext";
                item.VirtualPathNodes.Add(id1);
                SearchHandler.Instance.UpdateIndex(item);

                // id1/id2
                item = new IndexRequestItem(id2, IndexAction.Add);
                item.Title = "testing header";
                item.DisplayText = "testing introtext";
                item.VirtualPathNodes.Add(id1);
                item.VirtualPathNodes.Add(id2);
                SearchHandler.Instance.UpdateIndex(item);

                // id1/id3
                item = new IndexRequestItem(id3, IndexAction.Add);
                item.Title = "testing header";
                item.DisplayText = "testing introtext";
                item.VirtualPathNodes.Add(id1);
                item.VirtualPathNodes.Add(id3);
                SearchHandler.Instance.UpdateIndex(item);

                // id1/id3/id4
                item = new IndexRequestItem(id4, IndexAction.Add);
                item.Title = "testing header";
                item.DisplayText = "testing introtext";
                item.VirtualPathNodes.Add(id1);
                item.VirtualPathNodes.Add(id3);
                item.VirtualPathNodes.Add(id4);
                SearchHandler.Instance.UpdateIndex(item);

                // id1/id3/id4/id5
                item = new IndexRequestItem(id5, IndexAction.Add);
                item.Title = "testing header for id5";
                item.DisplayText = "testing introtext for id5";
                item.Metadata = "testing metadata for id5";
                item.VirtualPathNodes.Add(id1);
                item.VirtualPathNodes.Add(id3);
                item.VirtualPathNodes.Add(id4);
                item.VirtualPathNodes.Add(id5);
                SearchHandler.Instance.UpdateIndex(item);

                // id1/id3/id4/id5
                item = new IndexRequestItem(id5, IndexAction.Add);
                item.Title = "testing header for id5";
                item.DisplayText = "testing introtext for id5";
                item.Metadata = "testing metadata for id5";
                item.NamedIndex = "testindex2";
                item.VirtualPathNodes.Add(id1);
                item.VirtualPathNodes.Add(id3);
                item.VirtualPathNodes.Add(id4);
                item.VirtualPathNodes.Add(id5);
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                // id1/id3/id4
                VirtualPathQuery vpq = new VirtualPathQuery();
                vpq.VirtualPathNodes.Add(id1);
                vpq.VirtualPathNodes.Add(id3);
                vpq.VirtualPathNodes.Add(id4);

                SearchResults results = SearchHandler.Instance.GetSearchResults(vpq, 1, 20);
                Assert.AreEqual(2, results.TotalHits);

                // Test OR and AND
                VirtualPathQuery vpqOr1 = new VirtualPathQuery();
                vpqOr1.VirtualPathNodes.Add(id1);
                vpqOr1.VirtualPathNodes.Add(id3);
                vpqOr1.VirtualPathNodes.Add(id4);

                VirtualPathQuery vpqOr2 = new VirtualPathQuery();
                vpqOr2.VirtualPathNodes.Add(id4);

                GroupQuery gVpqOr = new GroupQuery(LuceneOperator.OR);
                gVpqOr.QueryExpressions.Add(vpqOr1);
                gVpqOr.QueryExpressions.Add(vpqOr2);

                results = SearchHandler.Instance.GetSearchResults(gVpqOr, 1, 20);
                Assert.AreEqual(2, results.TotalHits);

                gVpqOr = new GroupQuery(LuceneOperator.AND);
                gVpqOr.QueryExpressions.Add(vpqOr1);
                gVpqOr.QueryExpressions.Add(vpqOr2);

                results = SearchHandler.Instance.GetSearchResults(gVpqOr, 1, 20);
                Assert.AreEqual(0, results.TotalHits);

                //UPDATE id3 from id1/id3 -> id1/id2/id3
                item = new IndexRequestItem(id3, IndexAction.Update);
                item.AutoUpdateVirtualPath = true;
                item.Title = "testing header";
                item.DisplayText = "testing introtext";
                item.VirtualPathNodes.Add(id1);
                item.VirtualPathNodes.Add(id2);
                item.VirtualPathNodes.Add(id3);
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                // id1/id3/id4. No results for the old path
                vpq = new VirtualPathQuery();
                vpq.VirtualPathNodes.Add(id1);
                vpq.VirtualPathNodes.Add(id3);
                vpq.VirtualPathNodes.Add(id4);

                results = SearchHandler.Instance.GetSearchResults(vpq, 1, 20);
                Assert.AreEqual(0, results.TotalHits);

                // id1/id2/id3/id4. Should get results for the new path
                vpq = new VirtualPathQuery();
                vpq.VirtualPathNodes.Add(id1);
                vpq.VirtualPathNodes.Add(id2);
                vpq.VirtualPathNodes.Add(id3);
                vpq.VirtualPathNodes.Add(id4);

                Collection<string> namedIndexes = new Collection<string>();
                namedIndexes.Add("default");
                namedIndexes.Add("testindex2");
                results = SearchHandler.Instance.GetSearchResults(vpq, null, namedIndexes, 1, 20);
                Assert.AreEqual(3, results.TotalHits);

                //Check that autoupdated content is still searchable. 
                FieldQuery fq = new FieldQuery("\"metadata for id5\"");
                GroupQuery gq = new GroupQuery(LuceneOperator.AND);
                gq.QueryExpressions.Add(fq);
                gq.QueryExpressions.Add(vpq);
                results = SearchHandler.Instance.GetSearchResults(gq, 1, 20);
                Assert.AreEqual(1, results.TotalHits);

            }
            finally
            {

                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_AddUpdateReadWriteMultipleThreadTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            int numItems = 100;

            Collection<string> ids = new Collection<string>();
            for (int i = 0; i < numItems; i++)
            {
                ids.Add(Guid.NewGuid().ToString());
            }

            try
            {
                //Reset indexes
                ResetAllIndexes();

                // Start add thread
                Thread writeThread = new Thread(new ThreadStart(() =>
                {
                    for (int i = 0; i < numItems; i++)
                    {
                        SearchHandler.Instance.UpdateIndex(new IndexRequestItem(ids[i], IndexAction.Add));
                        FlushRequestQueue();
                    }
                }));

                writeThread.Start();

                // Start update thread
                Thread updateThread = new Thread(new ThreadStart(() =>
                {
                    for (int i = 0; i < numItems; i++)
                    {
                        SearchHandler.Instance.UpdateIndex(new IndexRequestItem(ids[i], IndexAction.Update));
                        FlushRequestQueue();
                    }
                }));

                updateThread.Start();

                // Start write thread
                Thread readThread = new Thread(new ThreadStart(() =>
                {
                    for (int i = 0; i < numItems; i++)
                    {
                        FieldQuery fq = new FieldQuery("test search in default index");
                        SearchHandler.Instance.GetSearchResults(fq, 1, 20);

                        fq = new FieldQuery(ids[i], Field.Id);
                        SearchHandler.Instance.GetSearchResults(fq, 1, 20);
                    }
                }));

                readThread.Start();

                writeThread.Join();
                readThread.Join();
                updateThread.Join();

                // Assert that all items are added
                for (int i = 0; i < numItems; i++)
                {
                    FieldQuery fq = new FieldQuery(ids[i], Field.Id);
                    SearchResults results = SearchHandler.Instance.GetSearchResults(fq, 1, 20);
                    Assert.AreEqual(1, results.IndexResponseItems.Count);
                }

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_AlotOfAddUpdateAndRemovesTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                IndexRequestItem item1 = null; ;
                IndexRequestItem item2 = null;
                IndexRequestItem item3 = null;

                for (int i = 0; i < 500; i++)
                {
                    string id = Guid.NewGuid().ToString();

                    IndexRequestItem item = new IndexRequestItem(id, IndexAction.Add);
                    item = new IndexRequestItem(id, IndexAction.Update);
                    item = new IndexRequestItem(id, IndexAction.Remove);

                    //Store the first, middle and last item for assert
                    if (i == 0)
                        item1 = item;
                    if (i == 250)
                        item2 = item;
                    if (i == 499)
                        item3 = item;

                    SearchHandler.Instance.UpdateIndex(item);
                }

                FlushRequestQueue();

                // Assert that items are removed
                FieldQuery fe = new FieldQuery(item1.Id, Field.Id);
                SearchResults results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count);

                fe = new FieldQuery(item2.Id, Field.Id);
                results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count);
                fe = new FieldQuery(item3.Id, Field.Id);

                results = SearchHandler.Instance.GetSearchResults(fe, 1, 20);
                Assert.AreEqual(0, results.IndexResponseItems.Count); 

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_AddTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                string id;
                
                id = Guid.NewGuid().ToString();
                IndexRequestItem item1 = new IndexRequestItem(id, IndexAction.Add);
                item1.DisplayText = "Hello World";
                SearchHandler.Instance.UpdateIndex(item1);

                id = Guid.NewGuid().ToString();
                IndexRequestItem item2 = new IndexRequestItem(id, IndexAction.Add);
                item2.DisplayText = "Hello\x1bTest";
                SearchHandler.Instance.UpdateIndex(item2);

                FlushRequestQueue();

                AssertEqualToSearchResult(item1, null);
                AssertEqualToSearchResult(item2, null);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_AlotOfAddTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                IndexRequestItem item1 = null; ;
                IndexRequestItem item2 = null;
                IndexRequestItem item3 = null;

                for (int i = 0; i < 500; i++)
                {
                    string id = Guid.NewGuid().ToString();

                    IndexRequestItem item = new IndexRequestItem(id, IndexAction.Add);

                    //Store the first, middle and last item for assert
                    if (i == 0)
                        item1 = item;
                    if (i == 250)
                        item2 = item;
                    if (i == 499)
                        item3 = item;

                    SearchHandler.Instance.UpdateIndex(item);
                }

                FlushRequestQueue();

                AssertEqualToSearchResult(item1, null);
                AssertEqualToSearchResult(item2, null);
                AssertEqualToSearchResult(item3, null);

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_TypeFieldTest()
        {
            string id = "1";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem(id, IndexAction.Add);
                item.Title = "testing header";
                item.DisplayText = "testing introtext";
                item.ItemType = "EPiServer.Common.Comment, EPiServer.Common";
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                FieldQuery q = new FieldQuery("EPiServer.Common*", Field.ItemType);
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);

                Assert.AreEqual(1, res.IndexResponseItems.Count);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_CultureFieldTest()
        {
            string id = "1";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem(id, IndexAction.Add);
                item.Title = "testing header";
                item.DisplayText = "testing introtext";
                item.Culture = "sv-SE";
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                FieldQuery q = new FieldQuery("sv-SE", Field.Culture);
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);

                q = new FieldQuery("sv*", Field.Culture);
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_AccessDeniedTest()
        {
            string id = "1";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem(id, IndexAction.Add);
                SearchHandler.Instance.UpdateIndex(item, "deniedService");

                FlushRequestQueue();

                FieldQuery q = new FieldQuery(id, Field.Id);
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, "deniedService", null, 1, 100);
                Assert.AreEqual(0, res.IndexResponseItems.Count);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_DefaultFieldSearchAfterAdd()
        {
            string id = "1";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem(id, IndexAction.Add);
                item.Title = "The title field";
                item.DisplayText = "The display text";
                item.Metadata = "The metadata field";

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                FieldQuery q = new FieldQuery("title");
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);

                q = new FieldQuery("display");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);

                q = new FieldQuery("metadata");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_DefaultFieldSearchAfterVirtualPathUpdate()
        {
            string id = "1";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                //Reset indexes
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem(id, IndexAction.Add);
                item.Title = "The title field";
                item.DisplayText = "The display text";
                item.Metadata = "The metadata field";
                item.VirtualPathNodes.Add("node");

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                FieldQuery q = new FieldQuery("title");
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);

                q = new FieldQuery("display");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);

                q = new FieldQuery("metadata");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);

                //Update
                item = new IndexRequestItem(id, IndexAction.Update);
                item.Title = "The title field";
                item.DisplayText = "The display text";
                item.Metadata = "The metadata field";
                item.VirtualPathNodes.Add("nodeupdate");

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                q = new FieldQuery("title");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);

                q = new FieldQuery("display");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);

                q = new FieldQuery("metadata");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_AddReferenceTest()
        {
            string id1 = "1";
            string id2 = "2";
            string id3 = "3";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();

                // Add the main item
                IndexRequestItem item = new IndexRequestItem(id1, IndexAction.Add);
                item.Title = "The title field";
                item.DisplayText = "The display text";
                item.Metadata = "The metadata field";

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                FieldQuery q = new FieldQuery("title AND display AND metadata");
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);

                // Add a reference item to main item
                IndexRequestItem refItem = new IndexRequestItem(id2, IndexAction.Add);
                refItem.Title = "referencetitle";
                refItem.DisplayText = "referencedisplay";
                refItem.Metadata = "referencemetadata";
                refItem.ReferenceId = id1;

                SearchHandler.Instance.UpdateIndex(refItem);

                FlushRequestQueue();

                // Make sure the old test still works
                q = new FieldQuery("title AND display AND metadata");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual(id1, res.IndexResponseItems[0].Id);

                // Make sure that we can search reference data and get the main item back
                q = new FieldQuery("referencetitle AND referencedisplay AND referencemetadata");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual(id1, res.IndexResponseItems[0].Id);

                // Add another reference item to main item
                IndexRequestItem refItem2 = new IndexRequestItem(id3, IndexAction.Add);
                refItem2.Title = "referencetitle second";
                refItem2.DisplayText = "referencedisplay second";
                refItem2.Metadata = "referencemetadata second";
                refItem2.ReferenceId = id1;

                SearchHandler.Instance.UpdateIndex(refItem2);

                FlushRequestQueue();

                // Make sure the old test still works
                q = new FieldQuery("title AND display AND metadata");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual(id1, res.IndexResponseItems[0].Id);

                // Make sure that we still can search the first reference data and get the main item back
                q = new FieldQuery("referencetitle AND referencedisplay AND referencemetadata");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual(id1, res.IndexResponseItems[0].Id);

                // Make sure that we can search second reference data and get the main item back
                q = new FieldQuery("\"referencetitle second\" AND \"referencedisplay second\" AND \"referencemetadata second\"");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual(id1, res.IndexResponseItems[0].Id);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_AddReferenceWithMissingMainItemTest()
        {
            string id1 = "1";
            string id2 = "2";
            string id3 = "3";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                RequestQueueHandler.TruncateQueue();

                //Reset indexes
                ResetAllIndexes();
                
                // Add a reference item to a non-existing main item
                IndexRequestItem refItem = new IndexRequestItem(id2, IndexAction.Add);
                refItem.Title = "referencetitle";
                refItem.DisplayText = "referencedisplay";
                refItem.Metadata = "referencemetadata";
                refItem.ReferenceId = id1;

                SearchHandler.Instance.UpdateIndex(refItem);

                FlushRequestQueue();

                // THEN, add the main item
                IndexRequestItem item = new IndexRequestItem(id1, IndexAction.Add);
                item.Title = "The title field";
                item.DisplayText = "The display text";
                item.Metadata = "The metadata field";

                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                FieldQuery q = new FieldQuery("title AND display AND metadata");
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);

                // Make sure the old test still works
                q = new FieldQuery("title AND display AND metadata");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual(id1, res.IndexResponseItems[0].Id);

                // Make sure that we can search reference data and get the main item back
                q = new FieldQuery("referencetitle AND referencedisplay AND referencemetadata");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual(id1, res.IndexResponseItems[0].Id);

                // Add another reference item to main item
                IndexRequestItem refItem2 = new IndexRequestItem(id3, IndexAction.Add);
                refItem2.Title = "referencetitle second";
                refItem2.DisplayText = "referencedisplay second";
                refItem2.Metadata = "referencemetadata second";
                refItem2.ReferenceId = id1;

                SearchHandler.Instance.UpdateIndex(refItem2);

                FlushRequestQueue();

                // Make sure the old test still works
                q = new FieldQuery("title AND display AND metadata");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual(id1, res.IndexResponseItems[0].Id);

                // Make sure that we still can search the first reference data and get the main item back
                q = new FieldQuery("referencetitle AND referencedisplay AND referencemetadata");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual(id1, res.IndexResponseItems[0].Id);

                // Make sure that we can search second reference data and get the main item back
                q = new FieldQuery("\"referencetitle second\" AND \"referencedisplay second\" AND \"referencemetadata second\"");
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual(id1, res.IndexResponseItems[0].Id);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_AssertReferenceDataUpdate()
        {
            string id1 = "1";
            string id2 = "2";
            string id3 = "3";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                RequestQueueHandler.TruncateQueue();

                // Reset indexes
                ResetAllIndexes();

                // Add main item
                IndexRequestItem item1 = new IndexRequestItem(id1, IndexAction.Add);
                item1.Title = "The maintitle field";
                item1.DisplayText = "The maindisplay text";
                item1.Metadata = "The mainmetadata field";

                // Add first reference item
                IndexRequestItem item2 = new IndexRequestItem(id2, IndexAction.Add);
                item2.Title = "The first reference title field";
                item2.DisplayText = "The first reference display text";
                item2.Metadata = "The first reference metadata field";
                item2.ReferenceId = id1;

                // Add second reference item
                IndexRequestItem item3 = new IndexRequestItem(id3, IndexAction.Add);
                item3.Title = "The second reference title field";
                item3.DisplayText = "The second reference display text";
                item3.Metadata = "The second reference metadata field";
                item3.ReferenceId = id1;

                SearchHandler.Instance.UpdateIndex(item1);
                SearchHandler.Instance.UpdateIndex(item2);
                SearchHandler.Instance.UpdateIndex(item3);

                FlushRequestQueue();

                // Update first reference
                item2 = new IndexRequestItem(id2, IndexAction.Update);
                item2.Title = "The first updated reference title field";
                item2.DisplayText = "The first updated reference display text";
                item2.Metadata = "The first updated reference metadata field";
                //item2.ReferenceId = id1;

                SearchHandler.Instance.UpdateIndex(item2);

                FlushRequestQueue();

                // Make sure that id1 and id3 is still searchable
                FieldQuery q = new FieldQuery("maintitle AND \"second reference\"");
                SearchResults r = SearchHandler.Instance.GetSearchResults(q, 1, 10);
                Assert.AreEqual(1, r.TotalHits);
                Assert.AreEqual(1, r.IndexResponseItems.Count);

                // Make sure that id2 has been updated
                q = new FieldQuery("\"first updated reference\"");
                r = SearchHandler.Instance.GetSearchResults(q, 1, 10);
                Assert.AreEqual(1, r.TotalHits);
                Assert.AreEqual(1, r.IndexResponseItems.Count);
                Assert.AreEqual(id1, r.IndexResponseItems[0].Id);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_AssertReferenceDataRemoval()
        {
            string id1 = "1";
            string id2 = "2";
            string id3 = "3";
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                RequestQueueHandler.TruncateQueue();

                // Reset indexes
                ResetAllIndexes();

                // Add main item
                IndexRequestItem item1 = new IndexRequestItem(id1, IndexAction.Add);
                item1.Title = "The maintitle field";
                item1.DisplayText = "The maindisplay text";
                item1.Metadata = "The mainmetadata field";

                // Add first reference item
                IndexRequestItem item2 = new IndexRequestItem(id2, IndexAction.Add);
                item2.Title = "The first reference title field";
                item2.DisplayText = "The first reference display text";
                item2.Metadata = "The first reference metadata field";
                item2.ReferenceId = id1;

                // Add second reference item
                IndexRequestItem item3 = new IndexRequestItem(id3, IndexAction.Add);
                item3.Title = "The second reference title field";
                item3.DisplayText = "The second reference display text";
                item3.Metadata = "The second reference metadata field";
                item3.ReferenceId = id1;

                SearchHandler.Instance.UpdateIndex(item1);
                SearchHandler.Instance.UpdateIndex(item2);
                SearchHandler.Instance.UpdateIndex(item3);

                FlushRequestQueue();

                // Remove second reference
                IndexRequestItem delItem = new IndexRequestItem(id2, IndexAction.Remove);
                //delItem.ReferenceId = id1;
                SearchHandler.Instance.UpdateIndex(delItem);

                FlushRequestQueue();

                // Make sure that id1 and id3 is still searchable
                FieldQuery q = new FieldQuery("maintitle AND \"second reference\"");
                SearchResults r = SearchHandler.Instance.GetSearchResults(q, 1, 10);
                Assert.AreEqual(1, r.TotalHits);
                Assert.AreEqual(1, r.IndexResponseItems.Count);

                // Make sure that id2 is not searchable
                q = new FieldQuery("\"first reference\"");
                r = SearchHandler.Instance.GetSearchResults(q, 1, 10);
                Assert.AreEqual(0, r.TotalHits);
                Assert.AreEqual(0, r.IndexResponseItems.Count);

                // Remove main item
                IndexRequestItem mainItem = new IndexRequestItem(id1, IndexAction.Remove);
                SearchHandler.Instance.UpdateIndex(mainItem);

                FlushRequestQueue();

                // Make sure that id3 is not searchable
                q = new FieldQuery("\"second reference\"");
                r = SearchHandler.Instance.GetSearchResults(q, 1, 10);
                Assert.AreEqual(0, r.TotalHits);
                Assert.AreEqual(0, r.IndexResponseItems.Count);

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_PublicationEndFieldTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                // Reset indexes
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem("1", IndexAction.Add);
                item.Title = "testing header";
                item.DisplayText = "testing introtext";
                item.PublicationEnd = DateTime.Now.AddSeconds(-1);
                SearchHandler.Instance.UpdateIndex(item);

                item = new IndexRequestItem("2", IndexAction.Add);
                item.Title = "testing header2";
                item.DisplayText = "testing introtext2";
                item.PublicationEnd = DateTime.Now.AddMinutes(5);
                SearchHandler.Instance.UpdateIndex(item);

                item = new IndexRequestItem("3", IndexAction.Add);
                item.Title = "testing header3";
                item.DisplayText = "testing introtext3";
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();


                FieldQuery q = new FieldQuery("testing", Field.Title);
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(2, res.IndexResponseItems.Count);
                Assert.AreEqual(2, res.IndexResponseItems.Count<IndexResponseItem>(iri => (iri.Id == "2" || iri.Id == "3") &&
                                                                 (iri.PublicationEnd == null || iri.PublicationEnd > DateTime.Now)));
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_PublicationStartFieldTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                // Reset indexes
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem("1", IndexAction.Add);
                item.Title = "testing header";
                item.DisplayText = "testing introtext";
                item.PublicationStart = DateTime.Now.AddMinutes(5);

                // Check that difference is less than a second (10 million ticks)
                Assert.IsTrue(Math.Abs(DateTime.Now.AddMinutes(5).Ticks-item.PublicationStart.Value.Ticks) < 10000*1000); 

                SearchHandler.Instance.UpdateIndex(item);

                item = new IndexRequestItem("2", IndexAction.Add);
                item.Title = "testing header2";
                item.DisplayText = "testing introtext2";
                item.PublicationStart = DateTime.Now;
                SearchHandler.Instance.UpdateIndex(item);

                item = new IndexRequestItem("3", IndexAction.Add);
                item.Title = "testing header3";
                item.DisplayText = "testing introtext3";
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();


                FieldQuery q = new FieldQuery("testing", Field.Title);
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(2, res.IndexResponseItems.Count);
                Assert.AreEqual(2, res.IndexResponseItems.Count<IndexResponseItem>(iri => (iri.Id == "2" || iri.Id == "3") &&
                                                                                    (iri.PublicationStart == null || iri.PublicationStart <= DateTime.Now)));
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_PublicationStartAndEndFieldTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                // Reset indexes
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem("1", IndexAction.Add);
                item.Title = "testing header";
                item.DisplayText = "testing introtext";
                item.PublicationStart = DateTime.Now.AddMinutes(5);
                item.PublicationEnd = DateTime.Now.AddMinutes(20);
                SearchHandler.Instance.UpdateIndex(item);

                item = new IndexRequestItem("2", IndexAction.Add);
                item.Title = "testing header2";
                item.DisplayText = "testing introtext2";
                item.PublicationStart = DateTime.Now.AddMinutes(-20);
                item.PublicationEnd = DateTime.Now.AddMinutes(-5);
                SearchHandler.Instance.UpdateIndex(item);

                item = new IndexRequestItem("3", IndexAction.Add);
                item.Title = "testing header3";
                item.DisplayText = "testing introtext3";
                item.PublicationStart = DateTime.Now.AddMinutes(-20);
                item.PublicationEnd = DateTime.Now.AddMinutes(5);
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();


                FieldQuery q = new FieldQuery("testing", Field.Title);
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual("3", res.IndexResponseItems[0].Id);
            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_ItemStatusTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                // Reset indexes
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem("1", IndexAction.Add);
                item.Title = "testing header";
                item.DisplayText = "testing introtext";
                item.ItemStatus = ItemStatus.Approved;
                SearchHandler.Instance.UpdateIndex(item);

                item = new IndexRequestItem("2", IndexAction.Add);
                item.Title = "testing header2";
                item.DisplayText = "testing introtext2";
                item.ItemStatus = ItemStatus.Pending;
                SearchHandler.Instance.UpdateIndex(item);

                item = new IndexRequestItem("3", IndexAction.Add);
                item.Title = "testing header3";
                item.DisplayText = "testing introtext3";
                item.ItemStatus = ItemStatus.Removed;
                SearchHandler.Instance.UpdateIndex(item);

                item = new IndexRequestItem("4", IndexAction.Add);
                item.Title = "testing header4";
                item.DisplayText = "testing introtext4";
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();

                IQueryExpression q = new ItemStatusQuery(ItemStatus.Approved);
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(2, res.IndexResponseItems.Count);
                Assert.AreEqual(2, res.IndexResponseItems.Count<IndexResponseItem>(iri => (iri.Id == "1" || iri.Id == "4")));

                q = new ItemStatusQuery(ItemStatus.Pending);
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual("2", res.IndexResponseItems[0].Id);

                q = new ItemStatusQuery(ItemStatus.Removed);
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(1, res.IndexResponseItems.Count);
                Assert.AreEqual("3", res.IndexResponseItems[0].Id);

                q = new ItemStatusQuery(ItemStatus.Approved | ItemStatus.Pending);
                res = SearchHandler.Instance.GetSearchResults(q, 1, 100);
                Assert.AreEqual(3, res.IndexResponseItems.Count);
                Assert.AreEqual(3, res.IndexResponseItems.Count<IndexResponseItem>(iri => (iri.Id == "1" || iri.Id == "2" || iri.Id == "4")));

            }
            finally
            {
                sh1.Close();
            }
        }

        [TestMethod]
        public void SH_WildcardQueryCaseInsensitivityOnStandardFieldsTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                // Reset indexes
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem("1", IndexAction.Add);
                item.Title = "Testing"; // title is made into lower case by the analyzer and should be handled as case-insensitive
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();


                IQueryExpression q = new FieldQuery("TEST*"); // a wildcard query for this field should work even when case does not match
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);

                Assert.AreEqual(1, res.IndexResponseItems.Count);


                IQueryExpression q2 = new FieldQuery("test*"); // a wildcard query for this field should work even when case does not match
                SearchResults res2 = SearchHandler.Instance.GetSearchResults(q2, 1, 100);

                Assert.AreEqual(1, res2.IndexResponseItems.Count);
            }
            finally
            {
                sh1.Close();
            }
        }


        [TestMethod]
        public void SH_WildcardQueryCaseSensitivityOnSpecialFieldsTest()
        {
            //Setup services
            Uri baseAddress1 = null;
            ServiceHost sh1 = null;
            SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                // Reset indexes
                ResetAllIndexes();

                IndexRequestItem item = new IndexRequestItem("Testing", IndexAction.Add); // id is left as-is by the analyzer (no change to casing)
                SearchHandler.Instance.UpdateIndex(item);

                FlushRequestQueue();


                IQueryExpression q = new FieldQuery("TEST*", Field.Id); // a wildcard query for this field should work only when case matches
                SearchResults res = SearchHandler.Instance.GetSearchResults(q, 1, 100);

                Assert.AreEqual(0, res.IndexResponseItems.Count);


                IQueryExpression q2 = new FieldQuery("test*", Field.Id); // a wildcard query for this field should work only when case matches
                SearchResults res2 = SearchHandler.Instance.GetSearchResults(q2, 1, 100);

                Assert.AreEqual(0, res2.IndexResponseItems.Count);


                IQueryExpression q3 = new FieldQuery("Test*", Field.Id); // a wildcard query for this field should work only when case matches
                SearchResults res3 = SearchHandler.Instance.GetSearchResults(q3, 1, 100);

                Assert.AreEqual(1, res3.IndexResponseItems.Count);

            }
            finally
            {
                sh1.Close();
            }
        }


        #region Helper methods


        private void AssertEqualToSearchResult(IndexRequestItem item, string namedIndex)
        {
            Collection<string> namedIndexes = new Collection<string>();
            namedIndexes.Add(namedIndex);
            EscapedFieldQuery fe = new EscapedFieldQuery(item.Id, Field.Id);
            SearchResults results = SearchHandler.Instance.GetSearchResults(fe, null, namedIndexes, 1, 20);
            Assert.AreEqual(1, results.IndexResponseItems.Count, "Found no or too many search results, expected exactly one");
            IndexResponseItem resultItem = results.IndexResponseItems[0];

            AssertIndexItemEquality(item, resultItem);
        }

        private void AssertIndexItemEquality(IndexItemBase item1, IndexItemBase item2)
        {
            Assert.AreEqual(item1.Id, item2.Id, "Id not equal");
            Assert.AreEqual(item1.Title, item2.Title, "Header not equal");
            Assert.AreEqual(item1.DisplayText, item2.DisplayText, "Body not equal");
            Assert.AreEqual(item1.Created.ToString(), item2.Created.ToString(), "Created not equal");
            Assert.AreEqual(item1.Modified.ToString(), item2.Modified.ToString(), "Modified not equal");
            Assert.AreEqual(item1.ItemType, item2.ItemType, "Type not equal");
            Assert.AreEqual(item1.Culture, item2.Culture, "Culture not equal");
            Assert.AreEqual((item1.Uri != null) ? item1.Uri.ToString() : "", (item2.Uri != null) ? item2.Uri.ToString() : "", "Uri not equal");
            Assert.AreEqual((item1.DataUri != null) ? item1.DataUri.ToString() : "", (item2.DataUri != null) ? item2.DataUri.ToString() : "", "Data Uri not equal");
            Assert.AreEqual(item1.ReferenceId, item2.ReferenceId, "Reference ID not equal");
            Assert.AreEqual(item1.BoostFactor.ToString(), item2.BoostFactor.ToString(), "Boost factor not equal");
            //Assert.AreEqual(item1.MetaData, item2.MetaData, "MetaData not equal"); // Not in response

            string expectedNamedIndex = item1.NamedIndex;
            if (item1.NamedIndex == null || item1.NamedIndex == "")
                expectedNamedIndex = "default";

            Assert.AreEqual(expectedNamedIndex, item2.NamedIndex);

            //Assert RACL
            int i = 0;
            Assert.AreEqual(item1.AccessControlList.Count, item2.AccessControlList.Count, "RACL count mismatch");
            foreach (string rac in item1.AccessControlList)
            {
                Assert.AreEqual(rac, item2.AccessControlList[i], "RACL mismatch");
                i++;
            }

            //Assert categories
            i = 0;
            Assert.AreEqual(item1.Categories.Count, item2.Categories.Count, "Categories count mismatch");
            foreach (string category in item1.Categories)
            {
                Assert.AreEqual(category, item2.Categories[i], "Category mismatch");
                i++;
            }

            //Assert Virtual Path
            i = 0;
            Assert.AreEqual(item1.VirtualPathNodes.Count, item2.VirtualPathNodes.Count, "Virtual path nodes count mismatch");
            foreach (string node in item1.VirtualPathNodes)
            {
                Assert.AreEqual(node.Replace(" ", ""), item2.VirtualPathNodes[i], "Virtual Path mismatch");
                i++;
            }

            //Assert authors
            i = 0;
            Assert.AreEqual(item1.Authors.Count, item2.Authors.Count, "Authors count mismatch");
            foreach (string author in item1.Authors)
            {
                Assert.AreEqual(author, item2.Authors[i], "Author mismatch");
                i++;
            }
        }

        private void ResetAllIndexes()
        {
            Collection<string> indexes = SearchHandler.Instance.GetNamedIndexes();
            foreach (string name in indexes)
                SearchHandler.Instance.ResetIndex(name);
        }

        public static void SetupIndexingServiceHost(out Uri baseAddress, out ServiceHost sh)
        {
            sh = new WebServiceHost(typeof(IndexingService));
            baseAddress = sh.BaseAddresses[0]; // From application config

            ServiceMetadataBehavior smb = sh.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (smb == null)
            {
                smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                sh.Description.Behaviors.Add(smb);
            }
            else
            {
                smb.HttpGetEnabled = false;
            }
            ServiceDebugBehavior sdb = sh.Description.Behaviors.Find<ServiceDebugBehavior>();
            if (sdb == null)
            {
                sdb = new ServiceDebugBehavior();
                sdb.IncludeExceptionDetailInFaults = true;
                sh.Description.Behaviors.Add(sdb);
            }
            else
            {
                sdb.IncludeExceptionDetailInFaults = true;
            }
        }

        private void CreateMultipleRequests()
        {
            string id1 = Guid.NewGuid().ToString();
            string id2 = Guid.NewGuid().ToString();
            string id3 = Guid.NewGuid().ToString();
            string id4 = Guid.NewGuid().ToString();
            string id5 = Guid.NewGuid().ToString();
            string id6 = Guid.NewGuid().ToString();
            string id7 = Guid.NewGuid().ToString();

            //Add items to different indexes
            IndexRequestItem item = new IndexRequestItem(id1, IndexAction.Add);
            item.Title = "This is the header for id1 in default index";
            item.DisplayText = "Detta är data i body delen";
            item.Metadata = "Detta är data i meta data delen som testas lite svårt";
            item.ItemType = "EPiServer.Common.Comment, EPiServer.Common";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id2, IndexAction.Add);
            item.Title = "This is the header for id2 in default index";
            item.ItemType = "Cms";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id3, IndexAction.Add);
            item.Title = "This is the header for id3 in default index";
            item.DisplayText = "This is the intro text for id3 in default index";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id4, IndexAction.Add);
            item.Title = "This is the header for id4 in default index";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id5, IndexAction.Add);
            item.Title = "This is the header for id5 in default index";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id6, IndexAction.Add);
            item.Title = "This is the header for id6 in default index";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id7, IndexAction.Add);
            item.Title = "This is the header for id7 in default index";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id1, IndexAction.Add);
            item.Title = "This is the header for id1 in testindex2";
            item.NamedIndex = "testindex2";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id2, IndexAction.Add);
            item.Title = "This is the header for id2 in testindex2";
            item.NamedIndex = "testindex2";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id3, IndexAction.Add);
            item.Title = "This is the header for id3 in testindex2";
            item.NamedIndex = "testindex2";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id4, IndexAction.Add);
            item.Title = "This is the header for id4 in testindex3";
            item.NamedIndex = "testindex3";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id5, IndexAction.Add);
            item.Title = "This is the header for id5 in testindex3";
            item.NamedIndex = "testindex3";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id6, IndexAction.Add);
            item.Title = "This is the header for id6 in testindex3";
            item.NamedIndex = "testindex3";
            SearchHandler.Instance.UpdateIndex(item);

            item = new IndexRequestItem(id7, IndexAction.Add);
            item.Title = "This is the header for id7 in testindex3";
            item.NamedIndex = "testindex3";
            SearchHandler.Instance.UpdateIndex(item);
        }

        #endregion
    }
}