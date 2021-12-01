namespace EPiServer.Search.IndexingService.Configuration
{
    public class NamedIndexElement
    {
        public string Name { get; set; }

        public string DirectoryPath { get; set; }

        public bool ReadOnly { get; set; }

        public int PendingDeletesOptimizeThreshold { get; set; } = 100;

        public bool IdFieldInResponse { get; set; } = true;

        public bool UriFieldInResponse { get; set; } = true;

        public bool TitleFieldInResponse { get; set; } = true;

        public bool DisplayTextFieldInResponse { get; set; } = true;

        public bool CreatedFieldInResponse { get; set; } = true;

        public bool ModifiedFieldInResponse { get; set; } = true;

        public bool AuthorFieldInResponse { get; set; } = true;

        public bool CategoriesFieldInResponse { get; set; } = true;

        public bool AclFieldInResponse { get; set; } = true;

        public bool TypeFieldInResponse { get; set; } = true;

        public bool CultureFieldInResponse { get; set; } = true;

        public bool VirtualPathFieldInResponse { get; set; } = true;

        public bool PublicationEndFieldInResponse { get; set; } = true;

        public bool PublicationStartFieldInResponse { get; set; } = true;

        public bool MetadataFieldInResponse { get; set; } = false;

        public bool ReferenceFieldInResponse { get; set; } = true;

        public bool ItemStatusFieldInResponse { get; set; } = true;
    }
}
