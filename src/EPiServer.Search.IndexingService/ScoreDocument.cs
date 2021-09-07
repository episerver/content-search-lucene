using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lucene.Net.Documents;

namespace EPiServer.Search.IndexingService
{
    public class ScoreDocument
    {
        public ScoreDocument(Document document, float score)
        {
            Document = document;
            Score = score;
        }

        internal Document Document 
        {
            get;
            set;
        }

        internal float Score
        {
            get;
            set;
        }
    }
}