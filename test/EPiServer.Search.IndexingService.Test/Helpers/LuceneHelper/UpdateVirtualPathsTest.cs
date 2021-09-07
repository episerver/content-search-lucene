﻿using EPiServer.Logging.Compatibility;
using Lucene.Net.Documents;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EPiServer.Search.IndexingService.Test.Helpers.LuceneHelper
{
    [Trait(nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper), nameof(EPiServer.Search.IndexingService.Helpers.LuceneHelper.UpdateVirtualPaths))]
    public class UpdateVirtualPathsTest : LuceneHelperTestBase
    {
        [Fact]
        public void UpdateVirtualPaths_WhenNewVirtualPathIsEmpty_ShouldReturnFalse()
        {
            var classInstant = SetupMock();
            var result = classInstant.UpdateVirtualPaths("vp", "");
            Assert.False(result);
        }

        [Fact]
        public void UpdateVirtualPaths_WhenNewVirtualPathEqualToOldVirtualPath_ShouldReturnFalse()
        {
            var classInstant = SetupMock();
            var result = classInstant.UpdateVirtualPaths("vp", "vp");
            Assert.False(result);
        }

        [Fact]
        public void UpdateVirtualPaths_ShouldReturnTrue()
        {
            var logMock = new Mock<ILog>();
            IndexingServiceSettings.IndexingServiceServiceLog = logMock.Object;

            var namedIndexMock = new Mock<NamedIndex>("testindex1");
            var dir1 = new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index\Main");
            var dir2 = new System.IO.DirectoryInfo(@"c:\fake\App_Data\Index\Ref");

            IndexingServiceSettings.NamedIndexElements.Add(namedIndexMock.Object.Name, new Configuration.NamedIndexElement() { Name= namedIndexMock.Object.Name });
            IndexingServiceSettings.MaxHitsForReferenceSearch = 1;
            IndexingServiceSettings.MaxHitsForReferenceSearch = 1;
            IndexingServiceSettings.NamedIndexDirectories.Add(namedIndexMock.Object.Name, Lucene.Net.Store.FSDirectory.Open(dir1));
            IndexingServiceSettings.ReferenceIndexDirectories.Add(namedIndexMock.Object.Name, Lucene.Net.Store.FSDirectory.Open(dir2));
            IndexingServiceSettings.MainDirectoryInfos.Add(namedIndexMock.Object.Name, dir1);
            IndexingServiceSettings.ReferenceDirectoryInfos.Add(namedIndexMock.Object.Name, dir2);
            IndexingServiceSettings.ReaderWriterLocks.Add(namedIndexMock.Object.Name, new ReaderWriterLockSlim());

            int totalHits = 1;
            var docs = new Collection<ScoreDocument>();
            docs.Add(new ScoreDocument(new Document
            {
                new TextField(IndexingServiceSettings.TitleFieldName,"Title",Field.Store.YES),
                new TextField(IndexingServiceSettings.DisplayTextFieldName,"Body",Field.Store.YES),
                new TextField(IndexingServiceSettings.MetadataFieldName,"Meta",Field.Store.YES),
                new TextField(IndexingServiceSettings.NamedIndexFieldName,"testindex1",Field.Store.YES),
                new TextField(IndexingServiceSettings.IdFieldName,"1",Field.Store.YES),
                new TextField(IndexingServiceSettings.VirtualPathFieldName,"vp1",Field.Store.YES)
            }, 1));

            _documentHelperMock.Setup(x => x.SingleIndexSearch(It.IsAny<string>(), It.IsAny<NamedIndex>(), It.IsAny<int>(), out totalHits)).Returns(docs);

            var classInstant = SetupMock();
            var result = classInstant.UpdateVirtualPaths("vp1", "vp2");
            Assert.True(result);
        }
    }
}