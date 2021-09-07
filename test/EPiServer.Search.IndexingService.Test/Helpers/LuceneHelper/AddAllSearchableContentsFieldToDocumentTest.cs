using EPiServer.Search.IndexingService.Helpers;
using Lucene.Net.Documents;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.AddAllSearchableContentsFieldToDocument))]
    public class AddAllSearchableContentsFieldToDocumentTest : LuceneHelperTestBase
    {
        [Fact]
        public void AddAllSearchableContentsFieldToDocument_ShouldEqualWithInputValues()
        {
            var doc = new Document
            {
                new TextField(IndexingServiceSettings.IdFieldName,"Id",Field.Store.YES),
                new TextField(IndexingServiceSettings.TitleFieldName,"Title",Field.Store.YES),
                new TextField(IndexingServiceSettings.DisplayTextFieldName,"Body",Field.Store.YES),
                new TextField(IndexingServiceSettings.MetadataFieldName,"Meta",Field.Store.YES),
            };
            
            var namedIndex = new NamedIndex("NameIndexTest");

            var classInstant = SetupMock();
            
            classInstant.AddAllSearchableContentsFieldToDocument(doc, namedIndex);

            Assert.Contains("Title Body Meta",doc.Get(IndexingServiceSettings.DefaultFieldName));
        }
    }
}
