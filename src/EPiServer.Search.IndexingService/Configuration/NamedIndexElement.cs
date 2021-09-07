using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.IO;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EPiServer.Search.IndexingService.Configuration
{
    public class NamedIndexElement
    {
        public NamedIndexElement()
        {
        }

        public string Name { get; set; }

        public string DirectoryPath { get; set; }

        public bool ReadOnly { get; set; }

        public int PendingDeletesOptimizeThreshold { get; set; }

        public bool IdFieldInResponse { get; set; }

        public bool UriFieldInResponse { get; set; }

        public bool TitleFieldInResponse { get; set; }

        public bool DisplayTextFieldInResponse { get; set; }

        public bool CreatedFieldInResponse { get; set; }

        public bool ModifiedFieldInResponse { get; set; }

        public bool AuthorFieldInResponse { get; set; }

        public bool CategoriesFieldInResponse { get; set; }

        public bool AclFieldInResponse { get; set; }

        public bool TypeFieldInResponse { get; set; }

        public bool CultureFieldInResponse { get; set; }

        public bool VirtualPathFieldInResponse { get; set; }

        public bool PublicationEndFieldInResponse { get; set; }

        public bool PublicationStartFieldInResponse { get; set; }

        public bool MetadataFieldInResponse { get; set; }

        public bool ReferenceFieldInResponse { get; set; }

        public bool ItemStatusFieldInResponse { get; set; }
    }
}
