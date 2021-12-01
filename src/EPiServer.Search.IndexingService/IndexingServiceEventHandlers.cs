using System;
using Lucene.Net.Documents;

namespace EPiServer.Search.IndexingService
{

    public class InternalServerErrorEventArgs : EventArgs
    {
        public InternalServerErrorEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public string ErrorMessage
        {
            get;
            set;
        }
    }

    public class OptimizedEventArgs : EventArgs
    {
        public OptimizedEventArgs(string namedIndex)
        {
            NamedIndex = namedIndex;
        }

        public string NamedIndex
        {
            get;
            set;
        }
    }

    public class RemoveEventArgs : EventArgs
    {
        public RemoveEventArgs(string documentId, string namedIndex)
        {
            DocumentId = documentId;
            NamedIndex = namedIndex;
        }

        public string DocumentId
        {
            get;
            set;
        }

        public string NamedIndex
        {
            get;
            set;
        }
    }

    public class AddUpdateEventArgs : EventArgs
    {
        public AddUpdateEventArgs(Document document, string namedIndex)
        {
            Document = document;
            NamedIndex = namedIndex;
        }

        public Document Document
        {
            get;
            set;
        }

        public string NamedIndex
        {
            get;
            set;
        }
    }
}
