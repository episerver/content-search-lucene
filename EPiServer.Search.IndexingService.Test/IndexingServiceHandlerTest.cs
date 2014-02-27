using EPiServer.Search.IndexingService.Wcf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Web;
using EPiServer.Search;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using System;
using System.ServiceModel.Web;
using System.Threading;
using System.Globalization;
using System.Runtime.Serialization;
using System.Net;
using System.IO;
using System.Text;
using System.Xml;
using System.Web;
using MockupService;
using System.ServiceModel.Syndication;

namespace EPiServer.Search.IndexingService.Test
{   
    /// <summary>
    ///This is a test class for IndexingServiceHandlerTest and is intended
    ///to contain all IndexingServiceHandlerTest Unit Tests
    ///</summary>
    [TestClass()]
    [DeploymentItem(@"EPiServer.Cms.Core.sql", IntegrationTestFiles.SqlOutput)]
    public class IndexingServiceHandlerTest
    {
        string accessKey = "accessKey1";
        string secretAccessKey = "secretAccessKey1";
        string _ns = "EPiServer.Search.IndexingService";

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


        [TestMethod()]
        public void IS_AddTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();
            
            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                SearchableItem mockupItem = MockupEntityHandler.GetItem(id1);

                //Occurs when the indexing service has completed the addition to the index
                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                //Subscribe to index completed event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                AddToIndex(baseAddress1, baseAddress2, id1, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                AddToIndex(baseAddress1, baseAddress2, id1, 1, "");

                Assert.IsFalse(wh.WaitOne(2000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "");

                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "failover1");

                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "failover2");
  
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;
            }
            finally
            {
                if(sh1 != null)
                    sh1.Close();
                if(sh2 != null)
                    sh2.Close();
            }
        }  

        [TestMethod()]
        public void IS_GetNamedIndexesTest()
        {
            Uri baseAddress1 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();

            try
            {
                NamedIndexes results = GetNamedIndexes(baseAddress1.ToString());
                Assert.AreEqual("default", results.IndexNames[0]);
                Assert.AreEqual("testindex2", results.IndexNames[1]);
                Assert.AreEqual("testindex3", results.IndexNames[2]);
                Assert.AreEqual("otherlanguage1", results.IndexNames[3]);
                Assert.AreEqual("otherlanguage2", results.IndexNames[4]);
                Assert.AreEqual("otherlanguage3", results.IndexNames[5]);

            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_RemoveTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                SearchableItem mockupItem = MockupEntityHandler.GetItem(id1);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                //Occurs when the indexing service has completed the removal from the index
                RemoveEventHandler itemIndexRemoved = (object sender, string itemId, string namedIndex) =>
                {
                    wh.Set();
                };

                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                //Subscribe to index remove event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentRemoved += itemIndexRemoved;


                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                AddToIndex(baseAddress1, baseAddress2, id1, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "");

                // Remove item
                RemoveFromIndex(baseAddress1, id1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to remove from index.");

                //Make sure the item is removed by searching for it by ID
                SearchResults results = GetSearchResults(baseAddress1.ToString(), "epiSearchId:" + id1, "", 100);
                Assert.AreEqual(0, results.TotalHits); //No hits

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchId:" + id1, "failover1", 100);
                Assert.AreEqual(0, results.TotalHits); //No hits

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchId:" + id1, "failover2", 100);
                Assert.AreEqual(0, results.TotalHits); //No hits

                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentRemoved -= itemIndexRemoved;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_UpdateTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                SearchableItem mockupItem = MockupEntityHandler.GetItem(id1);
                SearchableItem updatedItem = MockupEntityHandler.GetItemUpdated(id1);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                // Start making requests to the index

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                AddToIndex(baseAddress1, baseAddress2, id1, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "");

                UpdateIndex(baseAddress1, baseAddress2, id1, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to update the index.");

                SearchResults results = GetSearchResults(baseAddress1.ToString(), "epiSearchId:" + id1, "", 100);
                Assert.AreEqual(1, results.TotalHits);
                SearchResultItem resultItem = results.SearchResultItems[0];
                Assert.AreEqual(updatedItem.ID, resultItem.ID);
                Assert.AreEqual(updatedItem.AuthorName, resultItem.AuthorName);
                Assert.AreEqual(updatedItem.Created, resultItem.Created);
                Assert.AreEqual(updatedItem.Modified, resultItem.Modified);
                Assert.AreEqual(updatedItem.Header, resultItem.Header);
                Assert.AreEqual(updatedItem.Type, resultItem.Type);
                Assert.AreEqual(updatedItem.Uri, resultItem.Uri);
                Assert.AreEqual(updatedItem.Body, resultItem.Body);

                //Make sure it was updated in replicated indexes
                results = GetSearchResults(baseAddress1.ToString(), "epiSearchId:" + id1, "failover1", 100);
                Assert.AreEqual(1, results.TotalHits);
                resultItem = results.SearchResultItems[0];
                Assert.AreEqual(updatedItem.ID, resultItem.ID);
                Assert.AreEqual(updatedItem.AuthorName, resultItem.AuthorName);
                Assert.AreEqual(updatedItem.Created, resultItem.Created);
                Assert.AreEqual(updatedItem.Modified, resultItem.Modified);
                Assert.AreEqual(updatedItem.Header, resultItem.Header);
                Assert.AreEqual(updatedItem.Type, resultItem.Type);
                Assert.AreEqual(updatedItem.Uri, resultItem.Uri);
                Assert.AreEqual(updatedItem.Body, resultItem.Body);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchId:" + id1, "failover2", 100);
                Assert.AreEqual(1, results.TotalHits);
                resultItem = results.SearchResultItems[0];
                Assert.AreEqual(updatedItem.ID, resultItem.ID);
                Assert.AreEqual(updatedItem.AuthorName, resultItem.AuthorName);
                Assert.AreEqual(updatedItem.Created, resultItem.Created);
                Assert.AreEqual(updatedItem.Modified, resultItem.Modified);
                Assert.AreEqual(updatedItem.Header, resultItem.Header);
                Assert.AreEqual(updatedItem.Type, resultItem.Type);
                Assert.AreEqual(updatedItem.Uri, resultItem.Uri);
                Assert.AreEqual(updatedItem.Body, resultItem.Body);

                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_DefaultSearchTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                // Start making requests to the index

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                AddToIndex(baseAddress1, baseAddress2, id1, 1, "");
            
                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 2
                AddToIndex(baseAddress1, baseAddress2, id2, 7, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");
                
                //Add item 3
                AddToIndex(baseAddress1, baseAddress2, id3, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Get some search results

                SearchResults results = null;

                results = GetSearchResults(baseAddress1.ToString(), "+entity1 +entity2", "", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "entity1 entity2", "", 100);
                Assert.AreEqual(2, results.TotalHits);


                results = GetSearchResults(baseAddress1.ToString(), "((\"Jan Johansson\")) AND ((entity1) AND epiSearchCategories:(entity1/cars/volvo))", "", 100);
                Assert.AreEqual(1, results.TotalHits);


                results = GetSearchResults(baseAddress1.ToString(), "epiSearchId:(" + id1 + ")", "", 100);
                Assert.AreEqual(1, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "(\"entity1 in default\")", "", 100);
                Assert.AreEqual(1, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "((\"Jan Johansson\"))", "", 100);
                Assert.AreEqual(2, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "(\"body for\")", "", 100);
                Assert.AreEqual(3, results.TotalHits);

                ////Tags
                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCategories:(entity1/cars/volvo) AND epiSearchCategories:(SITEID=2)", "", 100);
                Assert.AreEqual(1, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "(entity2) AND epiSearchCategories:(entity1/cars/volvo)", "", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "(entity2) OR epiSearchCategories:(entity1/cars/volvo)", "", 100);
                Assert.AreEqual(2, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "(entity1) AND epiSearchCategories:(entity1/cars/volvo)", "", 100);
                Assert.AreEqual(1, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "(entity3) AND ((entity1) AND epiSearchCategories:(entity1/cars/volvo))", "", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "((\"Jan Johansson\")) AND ((entity1) AND epiSearchCategories:(entity1/cars/volvo))", "", 100);
                Assert.AreEqual(1, results.TotalHits);

                //TODO: why doesn't this work
                results = GetSearchResults(baseAddress1.ToString(), "((\"Jan Johansson\")) OR ((entity1) AND epiSearchCategories:(entity1/cars/volvo))", "", 100);
                Assert.AreEqual(2, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCategories:(tag1) AND epiSearchCategories:(tag3)", "", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCategories:(tag1) AND epiSearchCategories:(tag2)", "", 100);
                Assert.AreEqual(1, results.TotalHits);
                Assert.AreEqual(id3, results.SearchResultItems[0].ID);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCategories:(tag1 tag2)", "", 100);
                Assert.AreEqual(1, results.TotalHits);
                Assert.AreEqual(id1, results.SearchResultItems[0].ID);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCategories:(tag1) AND epiSearchCategories:(tag2)", "", 100);
                Assert.AreEqual(1, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCategories:(tag1)", "", 100);
                Assert.AreEqual(2, results.TotalHits);

                //Metadata
                results = GetSearchResults(baseAddress1.ToString(), "epiSearchDefault:(\"This is a searchable attribute value\")", "", 100);
                Assert.AreEqual(2, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "Johan", "", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "(Johan*)", "", 100);
                Assert.AreEqual(2, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "Johan?son", "", 100);
                Assert.AreEqual(2, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "((epiSearchHeader:\"Body\"))", "", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "(epiSearchBody:Body)", "", 100);
                Assert.AreEqual(3, results.TotalHits);

                //Dates
                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCreated:[20010101 TO 20020101]", "", 100);
                Assert.AreEqual(1, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCreated:[20010101 TO 20020404]", "", 100);
                Assert.AreEqual(2, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCreated:[20020202 TO 20030304]", "", 100);
                Assert.AreEqual(2, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCreated:[20020203 TO 20030304]", "", 100);
                Assert.AreEqual(1, results.TotalHits);

                //Types
                results = GetSearchResults(baseAddress1.ToString(), "epiSearchType:(type2) AND \"test header\" AND epiSearchCreated:[20010101 TO 20050101]", "", 100);
                Assert.AreEqual(2, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchType:(type2) AND (\"test header\") AND epiSearchCreated:{20020101 TO 20021001}", "", 100);
                Assert.AreEqual(1, results.TotalHits);


                results = GetSearchResults(baseAddress1.ToString(), "epiSearchType:type1 OR (epiSearchType:type2 AND \"test header\" AND epiSearchCreated:[20010101 TO 20050101])", "", 100);
                Assert.AreEqual(3, results.TotalHits);

                //Culture
                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCulture:" + CultureInfo.CurrentCulture.Name, "", 100);
                Assert.AreEqual(3, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCulture:(sv*)", "", 100);
                Assert.AreEqual(3, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCulture:(sv-FI)", "", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "*", "", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "*", "testindex2", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "Johan", "testindex8", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchBody:(header AND \"body for\")", "", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchBody:(body AND for)", "", 100);
                Assert.AreEqual(3, results.TotalHits);

                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;

            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_MaxItemsTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";
            string id4 = "4";
            string id5 = "5";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                // Start making requests to the index

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                AddToIndex(baseAddress1, baseAddress2, id1, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 2
                AddToIndex(baseAddress1, baseAddress2, id2, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 3
                AddToIndex(baseAddress1, baseAddress2, id3, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 4
                AddToIndex(baseAddress1, baseAddress2, id4, 1, "testindex2");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 5
                AddToIndex(baseAddress1, baseAddress2, id5, 1, "testindex3");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Get some search results

                SearchResults results = null;

                results = GetSearchResults(baseAddress1.ToString(), "body", "default,testindex2,testindex3", 100);
                Assert.AreEqual(5, results.SearchResultItems.Count);

                results = GetSearchResults(baseAddress1.ToString(), "body", "default,testindex2,testindex3", 5);
                Assert.AreEqual(5, results.SearchResultItems.Count);

                results = GetSearchResults(baseAddress1.ToString(), "body", "default,testindex2,testindex3", 4);
                Assert.AreEqual(4, results.SearchResultItems.Count);

                results = GetSearchResults(baseAddress1.ToString(), "body", "default,testindex2,testindex3", 3);
                Assert.AreEqual(3, results.SearchResultItems.Count);

                results = GetSearchResults(baseAddress1.ToString(), "body", "default,testindex2,testindex3", 2);
                Assert.AreEqual(2, results.SearchResultItems.Count);

                results = GetSearchResults(baseAddress1.ToString(), "body", "default,testindex2,testindex3", 1);
                Assert.AreEqual(1, results.SearchResultItems.Count);

                results = GetSearchResults(baseAddress1.ToString(), "body", "default,testindex2,testindex3", 0);
                Assert.AreEqual(0, results.SearchResultItems.Count);


                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;

            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }


        [TestMethod()]
        public void IS_AddBoostFactorTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                AddToIndex(baseAddress1, baseAddress2, id1, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 2
                AddToIndex(baseAddress1, baseAddress2, id2, 1.2f, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 3
                AddToIndex(baseAddress1, baseAddress2, id3, 1.5f, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Get some search results

                SearchResults results = null;

                results = GetSearchResults(baseAddress1.ToString(), "\"body for\"", "", 100);
                Assert.AreEqual(3, results.TotalHits);
                Assert.AreEqual(id3, results.SearchResultItems[0].ID);
                Assert.AreEqual(id2, results.SearchResultItems[1].ID);
                Assert.AreEqual(id1, results.SearchResultItems[2].ID);

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                AddToIndex(baseAddress1, baseAddress2, id1, 2, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 2
                AddToIndex(baseAddress1, baseAddress2, id2, 1.2f, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 3
                AddToIndex(baseAddress1, baseAddress2, id3, 1.5f, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Get some search results

                results = GetSearchResults(baseAddress1.ToString(), "\"body for\"", "", 100);
                Assert.AreEqual(3, results.TotalHits);
                Assert.AreEqual(id1, results.SearchResultItems[0].ID);
                Assert.AreEqual(id3, results.SearchResultItems[1].ID);
                Assert.AreEqual(id2, results.SearchResultItems[2].ID);

                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;

            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        ///// <summary>
        ///// Known issue: It seems that the MultiSearcher used whenever there is another index than default, ignores the boostfactor.
        ///// </summary>
        [TestMethod()]
        public void IS_AddBoostFactorNamedIndexTest()
        {
            string id6 = "6";
            string id7 = "7";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                        wh.Set();
                };

                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                AddToIndex(baseAddress1, baseAddress2, id6, 1, "otherlanguage1");

                Assert.IsTrue(wh.WaitOne(20000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 2
                AddToIndex(baseAddress1, baseAddress2, id7, 5f, "otherlanguage1");

                Assert.IsTrue(wh.WaitOne(20000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");


                SearchResults results = null;

                results = GetSearchResults(baseAddress1.ToString(), "\"otherlanguage1\"", "otherlanguage1", 100);
                Assert.AreEqual(2, results.TotalHits);
                Assert.AreEqual(id7, results.SearchResultItems[0].ID);
                Assert.AreEqual(id6, results.SearchResultItems[1].ID);

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                AddToIndex(baseAddress1, baseAddress2, id6, 3f, "otherlanguage1");

                Assert.IsTrue(wh.WaitOne(20000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 2
                AddToIndex(baseAddress1, baseAddress2, id7, 1.2f, "otherlanguage1");

                Assert.IsTrue(wh.WaitOne(20000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                results = GetSearchResults(baseAddress1.ToString(), "\"otherlanguage1\"", "otherlanguage1", 100);
                Assert.AreEqual(2, results.TotalHits);
                Assert.AreEqual(id6, results.SearchResultItems[0].ID);
                Assert.AreEqual(id7, results.SearchResultItems[1].ID);

                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;

            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_UpdateBoostFactorTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                AddToIndex(baseAddress1, baseAddress2, id1, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 2
                AddToIndex(baseAddress1, baseAddress2, id2, 1.2f, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 3
                AddToIndex(baseAddress1, baseAddress2, id3, 1.5f, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Get some search results

                SearchResults results = null;

                results = GetSearchResults(baseAddress1.ToString(), "\"body for\"", "", 100);
                Assert.AreEqual(3, results.TotalHits);
                Assert.AreEqual(id3, results.SearchResultItems[0].ID);
                Assert.AreEqual(id2, results.SearchResultItems[1].ID);
                Assert.AreEqual(id1, results.SearchResultItems[2].ID);

                //update item 1
                UpdateIndex(baseAddress1, baseAddress2, id1, 5f, "");

                Assert.IsTrue(wh.WaitOne(20000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                results = GetSearchResults(baseAddress1.ToString(), "\"header\"", "", 100);
                Assert.AreEqual(3, results.TotalHits);
                Assert.AreEqual(id1, results.SearchResultItems[0].ID);
                Assert.AreEqual(id3, results.SearchResultItems[1].ID);
                Assert.AreEqual(id2, results.SearchResultItems[2].ID);

                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;

            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_NamedIndexesSearchTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";
            string id4 = "4";
            string id5 = "5";
            string id6 = "6";
            string id7 = "7";
            
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                        wh.Set();
                };

                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                // Start making requests to the index

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Add item 1
                AddToIndex(baseAddress1, baseAddress2, id1, 1.4f, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 2
                AddToIndex(baseAddress1, baseAddress2, id2, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 3
                AddToIndex(baseAddress1, baseAddress2, id3, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");
            
                //Add item 4
                AddToIndex(baseAddress1, baseAddress2, id4, 1.5f, "testindex2");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 5
                AddToIndex(baseAddress1, baseAddress2, id5, 1.2f, "testindex3");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 6
                AddToIndex(baseAddress1, baseAddress2, id6, 1, "otherlanguage2");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                //Add item 7
                AddToIndex(baseAddress1, baseAddress2, id7, 1, "otherlanguage2");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the indexing service failed to add to index.");

                SearchResults results = null;

                // Get some search results
                results = GetSearchResults(baseAddress1.ToString(), "(\"entity1 in default\")", "testindex1", 100); //Index does not exist
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "(\"entity1 in default\")", "testindex2,testindex3", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "(\"entity1 in default\")", "default,testindex2,testindex3", 100);
                Assert.AreEqual(1, results.TotalHits);
                Assert.AreEqual("default", results.SearchResultItems[0].IndexName);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCategories:(entity4/flowers)", "default,testindex3", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "epiSearchCategories:(entity4/flowers)", "default,testindex2,testindex3", 100);
                Assert.AreEqual(1, results.TotalHits);
                Assert.AreEqual("testindex2", results.SearchResultItems[0].IndexName);

                results = GetSearchResults(baseAddress1.ToString(), "(\"test header\") AND (\"Author Entity7\")", "default,testindex2,testindex3", 100);
                Assert.AreEqual(0, results.TotalHits);

                results = GetSearchResults(baseAddress1.ToString(), "(\"test header\") AND (\"Author Entity7\")", "default,testindex2,testindex3,otherlanguage2", 100);
                Assert.AreEqual(1, results.TotalHits);
                Assert.AreEqual("otherlanguage2", results.SearchResultItems[0].IndexName);


                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;

            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_CallbackAddQueueTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();
            baseAddress2 = new Uri(
                 string.Format("http://{0}:{1}/SearchableItemService",
                 System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).HostName, 8078));

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                SearchableItem mockupItem = MockupEntityHandler.GetItem(id1);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler callbackQueued = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {

                        wh.Set();
                };

                //Subscribe to callbacl enqueued event
                EPiServer.Search.IndexingService.Wcf.IndexingService.CallbackQueued += callbackQueued;
                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Add item 1
                AddToIndex(baseAddress1, baseAddress2, id1, 1, "");

                Assert.IsTrue(wh.WaitOne(30000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the callback queue failed.");

                //Setup the service when the callback was queued
                MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
                sh2.Open();

                Assert.IsTrue(wh.WaitOne(30000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the add failed.");

                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "");

                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "failover1");

                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "failover2");
                

                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.CallbackQueued -= callbackQueued;
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_CallbackUpdateQueueTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                SearchableItem mockupItem = MockupEntityHandler.GetItem(id1);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler callbackQueued = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                //Subscribe to callback enqueued event
                EPiServer.Search.IndexingService.Wcf.IndexingService.CallbackQueued += callbackQueued;
                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Add item 1
                AddToIndex(baseAddress1, baseAddress2, id1, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the add failed.");

                //Close the service after successful add
                sh2.Close();

                //Try to make an update
                UpdateIndex(baseAddress1, baseAddress2, id1, 1, "");

                Assert.IsTrue(wh.WaitOne(20000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the enqueue failed.");

                //Start the service again
                MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
                sh2.Open();

                Assert.IsTrue(wh.WaitOne(20000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the add failed.");

                SearchableItem updatedItem = MockupEntityHandler.GetItemUpdated(id1);

                //Make sure the item is updated by searching for it by ID
                SearchResults results = GetSearchResults(baseAddress1.ToString(), "epiSearchId:" + id1, "", 100);

                Assert.AreEqual(1, results.TotalHits);
                SearchResultItem resultItem = results.SearchResultItems[0];
                Assert.AreEqual(updatedItem.ID, resultItem.ID);
                Assert.AreEqual(updatedItem.AuthorName, resultItem.AuthorName);
                Assert.AreEqual(updatedItem.Created, resultItem.Created);
                Assert.AreEqual(updatedItem.Modified, resultItem.Modified);
                Assert.AreEqual(updatedItem.Header, resultItem.Header);
                Assert.AreEqual(updatedItem.Type, resultItem.Type);
                Assert.AreEqual(updatedItem.Uri, resultItem.Uri);
                Assert.AreEqual(updatedItem.Body, resultItem.Body);

                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.CallbackQueued -= callbackQueued;
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_CallbackAddNamedIndexQueueTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();
            baseAddress2 = new Uri(
                 string.Format("http://{0}:{1}/SearchableItemService",
                 System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).HostName, 8078));

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler callbackQueued = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                //Subscribe to callbacl enqueued event
                EPiServer.Search.IndexingService.Wcf.IndexingService.CallbackQueued += callbackQueued;
                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                // Start making requests to the index

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Add item 1
                AddToIndex(baseAddress1, baseAddress2, id1, 1, "testindex2");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the callback queue failed.");

                //Setup the service when the callback was queued
                MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
                sh2.Open();

                Assert.IsTrue(wh.WaitOne(20000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the add failed.");

                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "testindex2");

                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.CallbackQueued -= callbackQueued;
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_BulkAddNoPageTest()
        {
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                EventHandler bulkAddCompleted = (object sender, EventArgs e) =>
                {
                    wh.Set();
                };

                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.BulkAddCompleted += bulkAddCompleted;

                // Start making requests to the index

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Bulk add with no pages
                string callbackUri = baseAddress2.ToString() + String.Format("/entity/bulk/nopage/?page={0}", "{page}");
                BulkAdd(baseAddress1, callbackUri, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the bulk add failed.");

                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.BulkAddCompleted -= bulkAddCompleted;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_BulkAddOnePageTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                EventHandler bulkAddCompleted = (object sender, EventArgs e) =>
                {
                    wh.Set();
                };

                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.BulkAddCompleted += bulkAddCompleted;

                // Start making requests to the index

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Bulk add with one page
                string callbackUri = baseAddress2.ToString() + String.Format("/entity/bulk/onepage/?page={0}", "{page}");
                BulkAdd(baseAddress1, callbackUri, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the bulk add failed.");

                //make sure that everything is added
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id2, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id3, "");


                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.BulkAddCompleted -= bulkAddCompleted;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_BulkAddMorePagesTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";
            string id4 = "4";
            string id5 = "5";
            string id6 = "6";
            string id7 = "7";

            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                EventHandler bulkAddCompleted = (object sender, EventArgs e) =>
                {
                    wh.Set();
                };

                //Subscribe to bulk add completed event
                EPiServer.Search.IndexingService.Wcf.IndexingService.BulkAddCompleted += bulkAddCompleted;

                // Start making requests to the index

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Bulk add with multiple pages
                string callbackUri =  baseAddress2.ToString() + String.Format("/entity/bulk/morepages/?page={0}", "{page}");
                BulkAdd(baseAddress1, callbackUri, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the bulk add failed.");

                //Make sure everything was added
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id2, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id3, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id4, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id5, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id6, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id7, "");


                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.BulkAddCompleted -= bulkAddCompleted;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_BulkAddMorePagesNamedIndexTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";
            string id4 = "4";
            string id5 = "5";
            string id6 = "6";
            string id7 = "7";

            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                EventHandler bulkAddCompleted = (object sender, EventArgs e) =>
                {
                    wh.Set();
                };

                //Subscribe to index add event
                EPiServer.Search.IndexingService.Wcf.IndexingService.BulkAddCompleted += bulkAddCompleted;

                // Start making requests to the index

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Bulk add with multiple pages
                string callbackUri = baseAddress2.ToString() + String.Format("/entity/bulk/morepages/?page={0}", "{page}");
                BulkAdd(baseAddress1, callbackUri, 1, "testindex2");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the bulk add failed.");

                //Make sure everything was added
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "testindex2");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id2, "testindex2");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id3, "testindex2");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id4, "testindex2");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id5, "testindex2");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id6, "testindex2");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id7, "testindex2");


                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.BulkAddCompleted -= bulkAddCompleted;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_BulkAddQueueTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";
            string id4 = "4";
            string id5 = "5";
            string id6 = "6";
            string id7 = "7";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            sh1.Open();
            baseAddress2 = new Uri(
                 string.Format("http://{0}:{1}/SearchableItemService",
                 System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).HostName, 8078));

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                //Occurs when the indexing service has completed the removal from the index
                AddUpdateEventHandler callbackQueued = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                EventHandler bulkAddCompleted = (object sender, EventArgs e) =>
                {
                    wh.Set();
                };

                //Subscribe to enqueued event
                EPiServer.Search.IndexingService.Wcf.IndexingService.CallbackQueued += callbackQueued;

                //Subscribe to index bulk completed event
                EPiServer.Search.IndexingService.Wcf.IndexingService.BulkAddCompleted += bulkAddCompleted;

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Bulk add with multiple pages
                string callbackUri = baseAddress2.ToString() + String.Format("/entity/bulk/morepages/?page={0}", "{page}");
                BulkAdd(baseAddress1, callbackUri, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the enqueue failed.");

                //Setup the service when the callback was queued
                MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
                sh2.Open();

                Assert.IsTrue(wh.WaitOne(20000), "Timed out waiting for waithandle to be signaled. This is probably beacuse bulk add failed.");

                //Make sure everything was added
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id1, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id2, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id3, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id4, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id5, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id6, "");
                AssertIndexedItemIsEqualToMockup(baseAddress1.ToString(), id7, "");

                // Unsubscribe
                EPiServer.Search.IndexingService.Wcf.IndexingService.CallbackQueued -= callbackQueued;
                EPiServer.Search.IndexingService.Wcf.IndexingService.BulkAddCompleted -= bulkAddCompleted;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_DenyAddIndexTest() //TODO; remove test?
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                //Occurs when the indexing service has completed the addition to the index
                EventHandler modificationDenied = (object sender, EventArgs e) =>
                {
                    wh.Set();
                };

                //Subscribe to index completed event
                EPiServer.Search.IndexingService.Wcf.IndexingService.ModificationDenied += modificationDenied;

                // Start makiing requests to the index

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Add item
                AddToIndex(baseAddress1, baseAddress2, id1, 1, "otherlanguage3");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the modification denied event was not fired.");

                SearchResults results = GetSearchResults(baseAddress1.ToString(), "epiSearchId:" + id1, "otherlanguage3", 100);
                Assert.AreEqual(0, results.TotalHits);

                EPiServer.Search.IndexingService.Wcf.IndexingService.ModificationDenied -= modificationDenied;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }

        [TestMethod()]
        public void IS_OptimizeIndexIntervalTest()
        {
            string id1 = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            string id2 = "2";
            string id3 = "3";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                //Occurs when the indexing service has completed the addition to the index
                OptimizedEventHandler optimize = (object sender, string namedIndex) =>
                {
                    wh.Set();
                };

                //Subscribe to index completed event
                EPiServer.Search.IndexingService.Wcf.IndexingService.IndexOptimized += optimize;

                // Start makiing requests to the index

                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Add items. 3 before optimize
                AddToIndex(baseAddress1, baseAddress2, id1, 1, "");
                AddToIndex(baseAddress1, baseAddress2, id2, 1, "");
                AddToIndex(baseAddress1, baseAddress2, id3, 1, "");

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. This is probably beacuse the optimize event was not fired.");

                EPiServer.Search.IndexingService.Wcf.IndexingService.IndexOptimized -= optimize;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }


        [TestMethod()]
        public void IS_SecurityTest()
        {
            string id = "e00c464d-2d3c-4ad0-a4ad-356364313321";
            Uri baseAddress1 = null;
            Uri baseAddress2 = null;

            //Setup services
            ServiceHost sh1 = null;
            ServiceHost sh2 = null;
            MockupServiceHandler.SetupIndexingServiceHost(out baseAddress1, out sh1);
            MockupServiceHandler.SetupMockupSearchableItemService(out baseAddress2, out sh2);
            sh1.Open();
            sh2.Open();

            try
            {
                AutoResetEvent wh = new AutoResetEvent(false);

                //Occurs when the indexing service has completed the addition to the index
                AddUpdateEventHandler itemIndexAdded = (object sender, SyndicationItem item, string namedIndex) =>
                {
                    wh.Set();
                };

                //Subscribe to index completed event
                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded += itemIndexAdded;

                Console.WriteLine("baseAddress1: " + baseAddress1.ToString());
                Console.WriteLine("baseAddress2: " + baseAddress2.ToString());

                // Start making requests to the index
                // Reset all named indexes
                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Add items that should work
                string callbackUri = baseAddress2.ToString() + String.Format("/entity/{0}", id);
                string sign = Mockup.GetSignature(accessKey, secretAccessKey, id, callbackUri, "1", "");

                Mockup.MakeHttpRequest(baseAddress1.ToString() + String.Format("/add/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}&accesskey={4}&signature={5}",
                    HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode("1"), HttpUtility.UrlEncode(""),
                    HttpUtility.UrlEncode(accessKey), HttpUtility.UrlEncode(sign)), "POST", null);

                Assert.IsTrue(wh.WaitOne(10000), "Timed out waiting for waithandle to be signaled. Document was not added.");

                Mockup.ResetAllNamedIndexes(baseAddress1);

                //Add without providing access key
                Mockup.MakeHttpRequest(baseAddress1.ToString() + String.Format("/add/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}&signature={4}",
                    HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode("1"), HttpUtility.UrlEncode(""),
                    HttpUtility.UrlEncode(sign)), "POST", null);

                Assert.IsFalse(wh.WaitOne(2000), "Waithandle was incorrectly signaled; document was added when it shouldn't have.");

                //Add without providing sign
                Mockup.MakeHttpRequest(baseAddress1.ToString() + String.Format("/add/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}&accesskey={4}",
                   HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode("1"), HttpUtility.UrlEncode(""),
                   HttpUtility.UrlEncode(accessKey)), "POST", null);

                Assert.IsFalse(wh.WaitOne(2000), "Waithandle was incorrectly signaled; document was added when it shouldn't have.");

                //Add with a faulty signature
                Mockup.MakeHttpRequest(baseAddress1.ToString() + String.Format("/add/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}&accesskey={4}&signature={5}",
                    HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode("1"), HttpUtility.UrlEncode(""),
                    HttpUtility.UrlEncode(accessKey), "faultysign"), "POST", null);

                Assert.IsFalse(wh.WaitOne(2000), "Waithandle was incorrectly signaled; document was added when it shouldn't have.");

                //Add with a non existing access key
                Mockup.MakeHttpRequest(baseAddress1.ToString() + String.Format("/add/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}&accesskey={4}&signature={5}",
                    HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode("1"), HttpUtility.UrlEncode(""),
                    "non existing access key", HttpUtility.UrlEncode(sign)), "POST", null);

                Assert.IsFalse(wh.WaitOne(2000), "Waithandle was incorrectly signaled; document was added when it shouldn't have.");

                //Add using access key with only read permission
                sign = Mockup.GetSignature("accessKey2", secretAccessKey, id, callbackUri, "1", "otherlanguage3");
                Mockup.MakeHttpRequest(baseAddress1.ToString() + String.Format("/add/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}&accesskey={4}&signature={5}",
                   HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode("1"), HttpUtility.UrlEncode(""),
                   HttpUtility.UrlEncode("accessKey2"), HttpUtility.UrlEncode(sign)), "POST", null);

                Assert.IsFalse(wh.WaitOne(2000), "Waithandle was incorrectly signaled; document was added when it shouldn't have.");

                //Add using access key with ip authentication. should match
                sign = Mockup.GetSignature("accessKey3", secretAccessKey, id, callbackUri, "1", "");
                Mockup.MakeHttpRequest(baseAddress1.ToString() + String.Format("/add/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}&accesskey={4}",
                   HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode("1"), HttpUtility.UrlEncode(""),
                   HttpUtility.UrlEncode("accessKey3"), HttpUtility.UrlEncode(sign)), "POST", null);

                Assert.IsTrue(wh.WaitOne(2000), "Timed out waiting for waithandle to be signaled. Document was not added.");

                //Add using access key with ip authentication no match
                sign = Mockup.GetSignature("accessKey3", secretAccessKey, id, callbackUri, "1", "");
                Mockup.MakeHttpRequest(baseAddress1.ToString() + String.Format("/add/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}&accesskey={4}",
                   HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode("1"), HttpUtility.UrlEncode(""),
                   HttpUtility.UrlEncode("accessKey4"), HttpUtility.UrlEncode(sign)), "POST", null);

                Assert.IsFalse(wh.WaitOne(2000), "Waithandle was incorrectly signaled; document was added when it shouldn't have.");

                //Add to a readonly index
                sign = Mockup.GetSignature(accessKey, secretAccessKey, id, callbackUri, "1", "otherlanguage3");
                Mockup.MakeHttpRequest(baseAddress1.ToString() + String.Format("/add/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}&accesskey={4}&signature={5}",
                    HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode("1"), HttpUtility.UrlEncode("otherlanguage3"),
                    HttpUtility.UrlEncode(accessKey), HttpUtility.UrlEncode(sign)), "POST", null);

                Assert.IsFalse(wh.WaitOne(2000), "Timed out waiting for waithandle to be signaled. Document was not added.");

                EPiServer.Search.IndexingService.Wcf.IndexingService.DocumentAdded -= itemIndexAdded;
            }
            finally
            {
                if (sh1 != null)
                    sh1.Close();
                if (sh2 != null)
                    sh2.Close();
            }
        }
       

        #region Helper methods

        private void AddItem()
        {
            string id = "2";

            Uri baseAddress1 = new Uri(
                 string.Format("http://{0}:{1}/IndexingService",
                 System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).HostName, 8079));


            Uri baseAddress2 = new Uri(
                 string.Format("http://{0}:{1}/SearchableItemService",
                 System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).HostName, 8078));

            Mockup.MakeHttpRequest(baseAddress1.ToString() + String.Format("/add/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}",
                    id, baseAddress2.ToString() + String.Format("/entity/{0}/{1}", id, 1), 1, ""), "POST", null);
        }

        private void AssertIndexedItemIsEqualToMockup(string baseAddress, string id, string namedIndex)
        {
            SearchableItem mockupItem = MockupEntityHandler.GetItem(id);

            if (String.IsNullOrEmpty(namedIndex))
                namedIndex = "default";

            SearchResults results = GetSearchResults(baseAddress.ToString(), "epiSearchId:" + id, namedIndex, 100);
            Assert.AreEqual(1, results.TotalHits);

            Assert.AreEqual(1, results.TotalHits);
            SearchResultItem resultItem = results.SearchResultItems[0];
            Assert.AreEqual(mockupItem.ID, resultItem.ID);
            Assert.AreEqual(mockupItem.AuthorName, resultItem.AuthorName);
            Assert.AreEqual(mockupItem.Modified, resultItem.Modified);
            Assert.AreEqual(mockupItem.Header, resultItem.Header);
            Assert.AreEqual(mockupItem.Type, resultItem.Type);
            Assert.AreEqual(mockupItem.Uri, resultItem.Uri);
            Assert.AreEqual(mockupItem.Body, resultItem.Body);
            Assert.AreEqual(namedIndex, resultItem.IndexName);
        }

        private SearchResults GetSearchResults(string baseAddress, string q, string namedIndexes, int maxItems)
        {
            string sign = Mockup.GetSignature(Mockup.AccessKey, Mockup.SecretAccessKey, q, namedIndexes, maxItems.ToString());

            XmlReader xmlReader = XmlReader.Create(baseAddress.ToString() +
                  String.Format("/search/?q={0}&namedIndexes={1}&maxitems={2}&format=xml&accesskey={3}&signature={4}", HttpUtility.UrlEncode(q), HttpUtility.UrlEncode(namedIndexes), maxItems, HttpUtility.UrlEncode(accessKey), HttpUtility.UrlEncode(sign)));

            SyndicationFeed feed = SyndicationFeed.Load(xmlReader);

            SearchResults results = new SearchResults();
            results.TotalHits = Int32.Parse(feed.AttributeExtensions[new XmlQualifiedName("TotalHits", _ns)]);

            foreach (SyndicationItem s in feed.Items)
            {
                SearchResultItem item = new SearchResultItem();
                item.ID = s.Id;
                item.Header = s.Title.Text;
                item.Body = ((TextSyndicationContent)s.Content).Text;
                item.Created = s.PublishDate.DateTime;
                item.Modified = s.LastUpdatedTime.DateTime;

                foreach(SyndicationPerson person in s.Authors)
                {
                    item.AuthorName += person.Name + " ";
                }
                item.AuthorName = item.AuthorName.Trim();

                item.Uri = s.BaseUri.ToString();
                item.Culture = s.AttributeExtensions[new XmlQualifiedName("Culture", _ns)];
                item.Type = s.AttributeExtensions[new XmlQualifiedName("Type", _ns)];
                item.IndexName = s.AttributeExtensions[new XmlQualifiedName("IndexName", _ns)];

                results.SearchResultItems.Add(item);
            }

            return results;
        }

        private NamedIndexes GetNamedIndexes(string baseAddress)
        {
            string sign = Mockup.GetSignature(Mockup.AccessKey, Mockup.SecretAccessKey);

            XmlReader xmlReader = XmlReader.Create(baseAddress.ToString() +
                  String.Format("/namedindexes/?accesskey={0}&signature={1}", HttpUtility.UrlEncode(accessKey), HttpUtility.UrlEncode(sign)));

            SyndicationFeed feed = SyndicationFeed.Load(xmlReader);
            NamedIndexes indexes = new NamedIndexes();
            foreach (SyndicationItem item in feed.Items)
            {
                indexes.IndexNames.Add(item.Title.Text);
            }
            return indexes;
        }


        private void AddToIndex(Uri baseAddressRequest, Uri baseAddressCallback, string id, float boostFactor, string namedIndex)
        {
            //Get signature
            string callbackUri = baseAddressCallback.ToString() + String.Format("/entity/{0}", id);
            string sign = Mockup.GetSignature(accessKey, secretAccessKey, id, callbackUri, boostFactor.ToString(), namedIndex);

            //Add item
            Mockup.MakeHttpRequest(baseAddressRequest.ToString() + String.Format("/add/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}&accesskey={4}&signature={5}",
                HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode(boostFactor.ToString()), HttpUtility.UrlEncode(namedIndex), 
                HttpUtility.UrlEncode(accessKey), HttpUtility.UrlEncode(sign)), "POST", null);
        }

        private void UpdateIndex(Uri baseAddressRequest, Uri baseAddressCallback, string id, float boostFactor, string namedIndex)
        {
            string callbackUri = baseAddressCallback.ToString() + String.Format("/entity/update/{0}", id);
            string sign = Mockup.GetSignature(accessKey, secretAccessKey, id, callbackUri, boostFactor.ToString(), "");

            Mockup.MakeHttpRequest(baseAddressRequest.ToString() + String.Format("/update/?id={0}&callbackuri={1}&boostfactor={2}&namedIndex={3}&accesskey={4}&signature={5}",
                    HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode(boostFactor.ToString()), HttpUtility.UrlEncode(namedIndex), 
                    HttpUtility.UrlEncode(accessKey), HttpUtility.UrlEncode(sign)), "POST", null);

        }

        private void RemoveFromIndex(Uri baseAddressRequest, string id, string namedIndex)
        {
            Mockup.MakeHttpRequest(baseAddressRequest.ToString() + String.Format("/delete/?id={0}&namedIndex={1}&accesskey={2}&signature={3}",
                    HttpUtility.UrlEncode(id), HttpUtility.UrlEncode(namedIndex), HttpUtility.UrlEncode(accessKey), 
                    HttpUtility.UrlEncode(Mockup.GetSignature(accessKey, secretAccessKey, id, ""))), "DELETE", null);
        }

        private void BulkAdd(Uri baseAddressRequest, string callbackUri, float boostFactor, string namedIndex)
        {
            
            string sign = Mockup.GetSignature(accessKey, secretAccessKey, callbackUri, boostFactor.ToString(), namedIndex);

            Mockup.MakeHttpRequest(baseAddressRequest.ToString() + String.Format("/add/bulk/?callbackuri={0}&boostfactor={1}&namedIndex={2}&accesskey={3}&signature={4}",
                HttpUtility.UrlEncode(callbackUri), HttpUtility.UrlEncode(boostFactor.ToString()), HttpUtility.UrlEncode(namedIndex), 
                HttpUtility.UrlEncode(accessKey), HttpUtility.UrlEncode(sign)), "POST", null);
        }

        #endregion
    }
}
