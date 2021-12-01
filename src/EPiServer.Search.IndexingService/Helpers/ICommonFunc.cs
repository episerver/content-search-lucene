using System;

namespace EPiServer.Search.IndexingService.Helpers
{
    public interface ICommonFunc
    {
        bool IsModifyIndex(string namedIndexName);
        string PrepareExpression(string q, bool excludeNotPublished);
        string PrepareEscapeFields(string q, string fieldName);
        bool IsValidIndex(string namedIndexName);
        void SplitDisplayTextToMetadata(string displayText, string metadata, out string displayTextOut, out string metadataOut);
        string GetNonFileUriContent(Uri uri);
        string GetFileUriContent(Uri uri);
        string GetFileText(string path);
    }
}
