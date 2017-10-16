﻿using System;
using System.Collections.Generic;
using System.Threading;
using EPiServer.Search.IndexingService.Configuration;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

namespace EPiServer.Search.IndexingService
{
    /// <summary>
    /// Class to encapsulates a named index, its per field analyzer and its field include in response flag
    /// </summary>
    internal class NamedIndex
    {
        #region Members
        private string _namedIndex;
        private Dictionary<string, bool> _fieldInResponse = new Dictionary<string,bool>();
        #endregion

        #region Constructors

        /// <summary>
        /// Contructs a <see cref="NamedIndex"/> for default Index
        /// </summary>
        internal NamedIndex()
            : this(null)
        {           
        }

        /// <summary>
        /// Constructs a <see cref="NamedIndex"/> for the passed name
        /// </summary>
        /// <param name="name"></param>
        internal NamedIndex(string name) : 
            this(name, false)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="NamedIndex"/> for the passed index name
        /// </summary>
        /// <param name="name">The name of the index for which to contruct a <see cref="NamedIndex"/></param>
        /// <param name="useRefIndex">Whether to costruct a <see cref="NamedIndex"/> for the reference index</param>
        internal NamedIndex(string name, bool useRefIndex)
        {
            _namedIndex = (String.IsNullOrEmpty(name)) ? IndexingServiceSettings.DefaultIndexName : name;

            if (IndexingServiceSettings.NamedIndexDirectories.ContainsKey(_namedIndex))
            {
                if (useRefIndex)
                {
                    Directory = (Lucene.Net.Store.Directory)IndexingServiceSettings.ReferenceIndexDirectories[_namedIndex];
                    ReferenceDirectoryInfo = IndexingServiceSettings.ReferenceDirectoryInfos[_namedIndex];
                }
                else
                {
                    Directory = (Lucene.Net.Store.Directory)IndexingServiceSettings.NamedIndexDirectories[_namedIndex];
                    ReferenceDirectory = (Lucene.Net.Store.Directory)IndexingServiceSettings.ReferenceIndexDirectories[_namedIndex];
                    ReferenceDirectoryInfo = IndexingServiceSettings.ReferenceDirectoryInfos[_namedIndex];
                    DirectoryInfo = IndexingServiceSettings.MainDirectoryInfos[_namedIndex];
                }

                NamedIndexElement element = (NamedIndexElement)IndexingServiceSettings.NamedIndexElements[_namedIndex];

                PendingDeletesOptimizeThreshold = element.PendingDeletesOptimizeThreshold;

                ReadOnly = element.ReadOnly;

                SetFieldInResponse(element);

                IsValid = true;
            }
        }

        #endregion

        #region Internal properties

        internal System.IO.DirectoryInfo DirectoryInfo
        {
            get;
            private set;
        }

        internal System.IO.DirectoryInfo ReferenceDirectoryInfo
        {
            get;
            private set;
        }

        internal bool IsValid
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Directory for this Index
        /// </summary>
        internal Lucene.Net.Store.Directory Directory
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the reference Directory for this Index where document data is stored separately
        /// </summary>
        internal Lucene.Net.Store.Directory ReferenceDirectory
        {
            get;
            private set;
        }

        internal bool IncludeInResponse(string defaultFieldName)
        {
            return _fieldInResponse.ContainsKey(defaultFieldName) ? _fieldInResponse[defaultFieldName] : false;
        }

        /// <summary>
        /// Gets and sets if this Index is readonly
        /// </summary>
        internal bool ReadOnly
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the name of this Index
        /// </summary>
        internal string Name
        {
            get
            {
                return _namedIndex;
            }
        }

        /// <summary>
        /// Gets the reference index name of this Index
        /// </summary>
        internal string ReferenceName
        {
            get
            {
                return _namedIndex + IndexingServiceSettings.RefIndexSuffix;
            }
        }

        /// <summary>
        /// Gets the number of pending deletes before running optimize on this Index
        /// </summary>
        internal int PendingDeletesOptimizeThreshold
        {
            get;
            set;
        }

        #endregion

        #region private

        private void SetFieldInResponse(NamedIndexElement element)
        {
            _fieldInResponse.Add(IndexingServiceSettings.DefaultFieldName, false);
            _fieldInResponse.Add(IndexingServiceSettings.IdFieldName, element.IdFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.TitleFieldName, element.TitleFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.DisplayTextFieldName, element.DisplayTextFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.AuthorsFieldName, element.AuthorFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.CreatedFieldName, element.CreatedFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.ModifiedFieldName, element.ModifiedFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.UriFieldName, element.UriFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.TypeFieldName, element.TypeFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.CultureFieldName, element.CultureFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.CategoriesFieldName, element.CategoriesFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.AclFieldName, element.AclFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.ReferenceIdFieldName, element.ReferenceFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.MetadataFieldName, element.MetadataFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.VirtualPathFieldName, element.VirtualPathFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.AuthorStorageFieldName, false);
            _fieldInResponse.Add(IndexingServiceSettings.PublicationEndFieldName, element.PublicationEndFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.PublicationStartFieldName, element.PublicationStartFieldInResponse);
            _fieldInResponse.Add(IndexingServiceSettings.ItemStatusFieldName, element.ItemStatusFieldInResponse);
        }

        #endregion
    }
}