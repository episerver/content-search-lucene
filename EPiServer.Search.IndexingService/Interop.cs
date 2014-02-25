using System;
using System.Text;
using System.Runtime.InteropServices;

namespace EPiServer.Search.IndexingService
{
    internal enum SummaryPropId : int
    {
        Title = 0x00000002,
        Subject = 0x00000003,
        Author = 0x00000004,
        Keywords = 0x00000005,
        Comments = 0x00000006,
        Template = 0x00000007,
        LastSavedBy = 0x00000008,
        RevisionNumber = 0x00000009,
        TotalEditingTime = 0x0000000A,
        LastPrinted = 0x0000000B,
        CreateDateTime = 0x0000000C,
        LastSaveDateTime = 0x0000000D,
        NumPages = 0x0000000E,
        NumWords = 0x0000000F,
        NumChars = 0x00000010,
        Thumbnail = 0x00000011,
        AppName = 0x00000012,
        Security = 0x00000013
    }

    internal enum STGC : int
    {
        DEFAULT = 0,
        OVERWRITE = 1,
        ONLYIFCURRENT = 2,
        DANGEROUSLYCOMMITMERELYTODISKCACHE = 4,
        CONSOLIDATE = 8
    }

    internal enum PROPSETFLAG : int
    {
        DEFAULT = 0,
        NONSIMPLE = 1,
        ANSI = 2,
        UNBUFFERED = 4,
        CASE_SENSITIVE = 8
    }

    internal enum STGM : int
    {
        READ = 0x00000000,
        WRITE = 0x00000001,
        READWRITE = 0x00000002,
        SHARE_DENY_NONE = 0x00000040,
        SHARE_DENY_READ = 0x00000030,
        SHARE_DENY_WRITE = 0x00000020,
        SHARE_EXCLUSIVE = 0x00000010,
        PRIORITY = 0x00040000,
        CREATE = 0x00001000,
        CONVERT = 0x00020000,
        FAILIFTHERE = 0x00000000,
        DIRECT = 0x00000000,
        TRANSACTED = 0x00010000,
        NOSCRATCH = 0x00100000,
        NOSNAPSHOT = 0x00200000,
        SIMPLE = 0x08000000,
        DIRECT_SWMR = 0x00400000,
        DELETEONRELEASE = 0x04000000
    }

    internal enum STGFMT : int
    {
        STORAGE = 0,
        FILE = 3,
        ANY = 4,
        DOCFILE = 5
    }

    [StructLayout(LayoutKind.Explicit, Size = 8, CharSet = CharSet.Unicode)]
    internal struct PropSpec
    {
        [FieldOffset(0)]
        internal int ulKind;
        [FieldOffset(4)]
        internal IntPtr Name_Or_ID;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct PropVariant
    {
        internal static PropVariant Empty
        {
            get
            {
                PropVariant p = new PropVariant();
                p.variantType = (short)VarEnum.VT_EMPTY;
                return p;
            }
        }

        [FieldOffset(0)]
        internal short variantType;
        [FieldOffset(8)]
        internal IntPtr pointerValue;
        [FieldOffset(8)]
        internal byte byteValue;
        [FieldOffset(8)]
        internal long longValue;

        internal void FromObject(object obj)
        {
            if (obj.GetType() == typeof(string))
            {
                this.variantType = (short)VarEnum.VT_LPWSTR;
                this.pointerValue = Marshal.StringToHGlobalUni((string)obj);
            }
        }

        public override string ToString()
        {
            VarEnum v = (VarEnum)variantType;
            if (pointerValue != IntPtr.Zero)
                return Marshal.PtrToStringUni(pointerValue);
            else
                return String.Empty;
        }

    }

    [ComVisible(true), ComImport(),
    Guid("0000013A-0000-0000-C000-000000000046"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertySetStorage
    {
        uint Create(
            [In, MarshalAs(UnmanagedType.Struct)] ref System.Guid rfmtid,
            [In] IntPtr pclsid,
            [In] int grfFlags,
            [In] int grfMode,
            ref IPropertyStorage propertyStorage);

        int Open(
            [In, MarshalAs(UnmanagedType.Struct)] ref System.Guid rfmtid,
            [In] int grfMode,
            ref IPropertyStorage propertyStorage);
    }

    [ComVisible(true), ComImport(),
    Guid("00000138-0000-0000-C000-000000000046"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStorage
    {
        int ReadMultiple(
            uint numProperties,
            [MarshalAs(UnmanagedType.Struct)] ref PropSpec propertySpecification,
            ref PropVariant propertyValues);

        int WriteMultiple(
            uint numProperties,
            [MarshalAs(UnmanagedType.Struct)] ref PropSpec
            propertySpecification,
            ref PropVariant propertyValues,
            int propIDNameFirst);

        uint Commit(
            int commitFlags);
    }

    internal enum HResults : uint
    {
        S_OK = 0,
        STG_E_FILEALREADYEXISTS = 0x80030050
    }

    internal class ole32
    {
        private ole32()
        {
        }

        [StructLayout(LayoutKind.Explicit, Size = 12,
             CharSet = CharSet.Unicode)]
        internal struct STGOptions
        {
            [FieldOffset(0)]
            ushort usVersion;
            [FieldOffset(2)]
            ushort reserved;
            [FieldOffset(4)]
            uint uiSectorSize;
            [FieldOffset(8), MarshalAs(UnmanagedType.LPWStr)]
            string
                pwcsTemplateFile;
        }

        [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint StgCreateStorageEx(
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            int accessMode, int storageFileFormat, int fileBuffering,
            IntPtr options, IntPtr reserved, ref System.Guid riid,
            [MarshalAs(UnmanagedType.Interface)] ref IPropertySetStorage
            propertySetStorage);

        [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint StgOpenStorageEx(
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            int accessMode, int storageFileFormat, int fileBuffering,
            IntPtr options, IntPtr reserved, ref System.Guid riid,
            [MarshalAs(UnmanagedType.Interface)] ref IPropertySetStorage
            propertySetStorage);
    }

    #region internal IFilter Enumerations
    /// <summary>
    /// Flags controlling the operation of the TextFilter
    /// instance.
    /// </summary>
    [Flags]
    internal enum IFILTER_INIT : int
    {
        /// <summary>
        /// Paragraph breaks should be marked with the Unicode PARAGRAPH SEPARATOR (0x2029)
        /// </summary>
        IFILTER_INIT_CANON_PARAGRAPHS = 1,
        /// <summary>
        /// Soft returns, such as the newline character in Microsoft® Word, should be replaced by hard returns—LINE SEPARATOR (0x2028). Existing hard returns can be doubled. A carriage return (0x000D), line feed (0x000A), or the carriage return and line feed in combination should be considered a hard return. The intent is to enable pattern-expression matches that match against observed line breaks. 
        /// </summary>
        IFILTER_INIT_HARD_LINE_BREAKS = 2,
        /// <summary>
        /// Various word-processing programs have forms of hyphens that are not represented in the host character set, such as optional hyphens (appearing only at the end of a line) and nonbreaking hyphens. This flag indicates that optional hyphens are to be converted to nulls, and non-breaking hyphens are to be converted to normal hyphens (0x2010), or HYPHEN-MINUSES (0x002D). 
        /// </summary>
        IFILTER_INIT_CANON_HYPHENS = 4,
        /// <summary>
        /// Just as the IFILTER_INIT_CANON_HYPHENS flag standardizes hyphens, this one standardizes spaces. All special space characters, such as nonbreaking spaces, are converted to the standard space character (0x0020). 
        /// </summary>
        IFILTER_INIT_CANON_SPACES = 8,
        /// <summary>
        /// Indicates that the client wants text split into chunks representing internal value-type properties. 
        /// </summary>
        IFILTER_INIT_APPLY_INDEX_ATTRIBUTES = 16,
        /// <summary>
        /// Indicates that the client wants text split into chunks representing properties determined during the indexing process. 
        /// </summary>
        IFILTER_INIT_APPLY_CRAWL_ATTRIBUTES = 256,
        /// <summary>
        /// Any properties not covered by the IFILTER_INIT_APPLY_INDEX_ATTRIBUTES and IFILTER_INIT_APPLY_CRAWL_ATTRIBUTES flags should be emitted. 
        /// </summary>
        IFILTER_INIT_APPLY_OTHER_ATTRIBUTES = 32,
        /// <summary>
        /// Optimizes IFilter for indexing because the client calls the IFilter::Init method only once and does not call IFilter::BindRegion. This eliminates the possibility of accessing a chunk both before and after accessing another chunk. 
        /// </summary>
        IFILTER_INIT_INDEXING_ONLY = 64,
        /// <summary>
        /// The text extraction process must recursively search all linked objects within the document. If a link is unavailable, the IFilter::GetChunk call that would have obtained the first chunk of the link should return FILTER_E_LINK_UNAVAILABLE. 
        /// </summary>
        IFILTER_INIT_SEARCH_LINKS = 128,
        /// <summary>
        /// The content indexing process can return property values set by the filter. 
        /// </summary>
        IFILTER_INIT_FILTER_OWNED_VALUE_OK = 512
    }

    /// <summary>
    /// Enumerates the different breaking types that occur between 
    /// chunks of text read out by the TextFilter.
    /// </summary>
    internal enum CHUNK_BREAKTYPE : uint
    {
        /// <summary>
        /// No break is placed between the current chunk and the previous chunk. The chunks are glued together. 
        /// </summary>
        CHUNK_NO_BREAK = 0,
        /// <summary>
        /// A word break is placed between this chunk and the previous chunk that had the same attribute. 
        /// Use of CHUNK_EOW should be minimized because the choice of word breaks is language-dependent, 
        /// so determining word breaks is best left to the search engine. 
        /// </summary>
        CHUNK_EOW = 1,
        /// <summary>
        /// A sentence break is placed between this chunk and the previous chunk that had the same attribute. 
        /// </summary>
        CHUNK_EOS = 2,
        /// <summary>
        /// A paragraph break is placed between this chunk and the previous chunk that had the same attribute.
        /// </summary>			
        CHUNK_EOP = 3,
        /// <summary>
        /// A chapter break is placed between this chunk and the previous chunk that had the same attribute. 
        /// </summary>
        CHUNK_EOC = 4
    }
    #endregion

    #region Internal IFilter Enumerations
    /// <summary>
    /// Types of properties returned by IFilter
    /// </summary>
    internal enum PROPSPECKIND : uint
    {
        /// <summary>
        /// Property is a string
        /// </summary>
        PRSPEC_LPWSTR = 0,
        /// <summary>
        /// Property is a property id
        /// </summary>
        PRSPEC_PROPID = 1
    }

    /// <summary>
    /// Types of chunks returned by IFilter
    /// </summary>
    internal enum CHUNKSTATE : uint
    {
        /// <summary>
        /// The current chunk is a text-type property.
        /// </summary>
        CHUNK_TEXT = 0x1,
        /// <summary>
        /// The current chunk is a value-type property. 
        /// </summary>
        CHUNK_VALUE = 0x2,
        /// <summary>
        /// Reserved
        /// </summary>
        CHUNK_FILTER_OWNED_VALUE = 0x4
    }
    #endregion

    #region Internal IFilter Types
    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct STAT_CHUNK
    {
        /// <summary>
        /// The chunk identifier. Chunk identifiers must be unique for the current instance of the IFilter interface. 
        /// Chunk identifiers must be in ascending order. The order in which chunks are numbered should correspond to the order in which they appear in the source document. Some search engines can take advantage of the proximity of chunks of various properties. If so, the order in which chunks with different properties are emitted will be important to the search engine. 
        /// </summary>
        internal int idChunk;
        /// <summary>
        /// The type of break that separates the previous chunk from the current chunk. Values are from the CHUNK_BREAKTYPE enumeration. 
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        internal CHUNK_BREAKTYPE breakType;
        /// <summary>
        /// Flags indicate whether this chunk contains a text-type or a value-type property. 
        /// Flag values are taken from the CHUNKSTATE enumeration. If the CHUNK_TEXT flag is set, 
        /// IFilter::GetText should be used to retrieve the contents of the chunk as a series of words. 
        /// If the CHUNK_VALUE flag is set, IFilter::GetValue should be used to retrieve 
        /// the value and treat it as a single property value. If the filter dictates that the same 
        /// content be treated as both text and as a value, the chunk should be emitted twice in two 
        /// different chunks, each with one flag set. 
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        internal CHUNKSTATE flags;
        /// <summary>
        /// The language and sublanguage associated with a chunk of text. Chunk locale is used 
        /// by document indexers to perform proper word breaking of text. If the chunk is 
        /// neither text-type nor a value-type with data type VT_LPWSTR, VT_LPSTR or VT_BSTR, 
        /// this field is ignored. 
        /// </summary>
        internal int locale;
        /// <summary>
        /// The property to be applied to the chunk. If a filter requires that the same text 
        /// have more than one property, it needs to emit the text once for each property 
        /// in separate chunks. 
        /// </summary>
        [MarshalAs(UnmanagedType.Struct)]
        internal FULLPROPSPEC attribute;
        /// <summary>
        /// The ID of the source of a chunk. The value of the idChunkSource member depends on the nature of the chunk: 
        /// If the chunk is a text-type property, the value of the idChunkSource member must be the same as the value of the idChunk member. 
        /// If the chunk is an internal value-type property derived from textual content, the value of the idChunkSource member is the chunk ID for the text-type chunk from which it is derived. 
        /// If the filter attributes specify to return only internal value-type properties, there is no content chunk from which to derive the current internal value-type property. In this case, the value of the idChunkSource member must be set to zero, which is an invalid chunk. 
        /// </summary>
        internal int idChunkSource;
        /// <summary>
        /// The offset from which the source text for a derived chunk starts in the source chunk. 
        /// </summary>
        internal int cwcStartSource;
        /// <summary>
        /// The length in characters of the source text from which the current chunk was derived. 
        /// A zero value signifies character-by-character correspondence between the source text and 
        /// the derived text. A nonzero value means that no such direct correspondence exists
        /// </summary>
        internal int cwcLenSource;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct FULLPROPSPEC
    {
        internal Guid guidPropSet;
        internal PropSpec psProperty;
    }

    #endregion

    /// <summary>
    /// A Managed Code class for invoking an Indexing Service IFilter
    /// object on a document to convert it to a text only representation.
    /// </summary>
    internal class TextFilter
    {
        [DllImport("query.dll", CharSet = CharSet.Unicode)]
        internal extern static int LoadIFilter(
            string pwcsPath,
            [MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter,
            ref IFilter ppIUnk
            );

        [DllImport("iprop.dll", CharSet = CharSet.Unicode)]
        internal extern static int PropVariantClear(IntPtr pvar);
    }

    [ComImport, Guid("00000000-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IUnknown
    {
        [PreserveSig]
        IntPtr QueryInterface(ref Guid riid, out IntPtr pVoid);

        [PreserveSig]
        IntPtr AddRef();

        [PreserveSig]
        IntPtr Release();
    }

    [ComImport, Guid("89BCB740-6119-101A-BCB7-00DD010655AF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFilter
    {
        /// <summary>
        /// The IFilter::Init method initializes a filtering session.
        /// </summary>
        [PreserveSig]
        IFilterReturnCodes Init(
            //[in] Flag settings from the IFILTER_INIT enumeration for controlling text standardization, property output, embedding scope, and IFilter access patterns. 
            [MarshalAs(UnmanagedType.U4)]
				IFILTER_INIT grfFlags,
            // [in] The size of the attributes array. When nonzero, cAttributes takes 
            // precedence over attributes specified in grfFlags. If no attribute flags 
            // are specified and cAttributes is zero, the default is given by the 
            // PSGUID_STORAGE storage property set, which contains the date and time 
            // of the last write to the file, size, and so on; and by the PID_STG_CONTENTS 
            // 'contents' property, which maps to the main contents of the file. 
            // For more information about properties and property sets, see Property Sets. 
            int cAttributes,
            //[in] Array of pointers to FULLPROPSPEC structures for the requested properties. 
            // When cAttributes is nonzero, only the properties in aAttributes are returned. 
            // pdwFlags 
            IntPtr aAttributes,
            // [out] Information about additional properties available to the caller; from the IFILTER_FLAGS enumeration. 
            [MarshalAs(UnmanagedType.U4)]
				ref IFILTER_FLAGS pdwFlags);

        /// <summary>
        /// The IFilter::GetChunk method positions the filter at the beginning of the next chunk, 
        /// or at the first chunk if this is the first call to the GetChunk method, and returns a description of the current chunk. 
        /// </summary>
        [PreserveSig]
        IFilterReturnCodes GetChunk([MarshalAs(UnmanagedType.Struct)] ref STAT_CHUNK pStat);

        /// <summary>
        /// The IFilter::GetText method retrieves text (text-type properties) from the current chunk, 
        /// which must have a CHUNKSTATE enumeration value of CHUNK_TEXT.
        /// </summary>
        [PreserveSig]
        IFilterReturnCodes GetText(
            // [in/out] On entry, the size of awcBuffer array in wide/Unicode characters. On exit, the number of Unicode characters written to awcBuffer. 
            // Note that this value is not the number of bytes in the buffer. 
            ref int pcwcBuffer,
            // Text retrieved from the current chunk. Do not terminate the buffer with a character.  
            [Out(), MarshalAs(UnmanagedType.LPWStr)] 
				StringBuilder awcBuffer
            );

        /// <summary>
        /// The IFilter::GetValue method retrieves a value (internal value-type property) from a chunk, 
        /// which must have a CHUNKSTATE enumeration value of CHUNK_VALUE.
        /// </summary>
        [PreserveSig]
        IFilterReturnCodes GetValue(
            // Allocate the PROPVARIANT structure with CoTaskMemAlloc. Some PROPVARIANT 
            // structures contain pointers, which can be freed by calling the PropVariantClear function. 
            // It is up to the caller of the GetValue method to call the PropVariantClear method.								
            ref IntPtr ppPropValue
            );

        /// <summary>
        /// The IFilter::BindRegion method retrieves an interface representing the specified portion of the object. 
        /// Currently reserved for future use.
        /// </summary>
        [PreserveSig]
        IFilterReturnCodes BindRegion(
            ref FILTERREGION origPos,
            ref Guid riid,
            ref IUnknown ppunk
            );

    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Auto)]
    internal struct PROPVARIANT
    {
        internal Int16 vt;
        internal Int16 wReserved1;
        internal Int16 wReserved2;
        internal Int16 wReserved3;
        internal IntPtr data;
    }
    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct FILTERREGION
    {
        internal int idChunk;
        internal int cwcStart;
        internal int cwcExtent;
    }

    internal enum VariantTypes
    {
        /// <summary>
        /// A property with a type indicator of VT_EMPTY has no data associated with it; that is, the size of the value is zero. 
        /// </summary>
        VT_EMPTY = 0,
        /// <summary>
        /// This is like a pointer to NULL. 
        /// </summary>
        VT_NULL = 1,
        /// <summary>
        /// cVal 1-byte signed integer. 
        /// </summary>
        VT_I1 = 16,
        /// <summary>
        /// bVal 1-byte unsigned integer. 
        /// </summary>
        VT_UI1 = 17,
        /// <summary>
        /// Two bytes representing a 2-byte signed integer value. 
        /// </summary>
        VT_I2 = 2,
        /// <summary>
        /// 2-byte unsigned integer. 
        /// </summary>
        VT_UI2 = 18,
        /// <summary>
        /// 4-byte signed integer value. 
        /// </summary>
        VT_I4 = 3,
        /// <summary>
        /// 4-byte signed integer value (equivalent to VT_I4). 
        /// </summary>
        VT_INT = 22,
        /// <summary>
        /// 4-byte unsigned integer. 
        /// </summary>
        VT_UI4 = 19,
        /// <summary>
        /// 4-byte unsigned integer (equivalent to VT_UI4). 
        /// </summary>
        VT_UINT = 23,
        /// <summary>
        /// 8-byte signed integer. 
        /// </summary>
        VT_I8 = 20,
        /// <summary>
        /// 8-byte unsigned integer. 
        /// </summary>
        VT_UI8 = 21,
        /// <summary>
        /// 32-bit IEEE floating point value. 
        /// </summary>
        VT_R4 = 4,
        /// <summary>
        /// 64-bit IEEE floating point value. 
        /// </summary>
        VT_R8 = 5,
        /// <summary>
        /// 8-byte two's complement integer (scaled by 10,000). This type is commonly used for currency amounts. 
        /// </summary>
        VT_CY = 6,
        /// <summary>
        /// A 64-bit floating point number representing the number of days (not seconds) since December 31, 1899. For example, January 1, 1900, is 2.0, January 2, 1900, is 3.0, and so on). This is stored in the same representation as VT_R8. 
        /// </summary>
        VT_DATE = 7,
        /// <summary>
        /// bstrVal Pointer to a null-terminated Unicode string. The string is immediately preceded 
        /// by a DWORD representing the byte count, but bstrVal points past this DWORD to 
        /// the first character of the string. BSTRs must be allocated and freed using the 
        /// Automation SysAllocString and SysFreeString calls. 
        /// </summary>
        VT_BSTR = 8,
        /// <summary>
        /// (bool in earlier designs) Boolean value, a WORD containing 0 (FALSE) or -1 (TRUE). 
        /// </summary>
        VT_BOOL = 11,
        /// <summary>
        /// A DWORD containing a status code. 
        /// </summary>
        VT_ERROR = 10,
        /// <summary>
        /// filetime 64-bit FILETIME structure as defined by Win32. It is recommended that all times be stored in Universal Coordinate Time (UTC). 
        /// </summary>
        VT_FILETIME = 64,
        /// <summary>
        /// Pointer to a null-terminated ANSI string in the system default code page. 
        /// </summary>
        VT_LPSTR = 30,
        /// <summary>
        /// Pointer to a null-terminated Unicode string in the user's default locale. 
        /// </summary>
        VT_LPWSTR = 31,
        /// <summary>
        /// Pointer to a class identifier (CLSID) (or other globally unique identifier (GUID)). 
        /// </summary>
        VT_CLSID = 72,
        /// <summary>
        /// Pointer to a clipdata structure
        /// </summary>
        VT_CF = 71,
        /// <summary>
        /// DWORD count of bytes, followed by that many bytes of data. The byte 
        /// count does not include the four bytes for the length of the count itself; 
        /// an empty blob member would have a count of zero, followed by zero bytes. 
        /// This is similar to the value VT_BSTR but does not guarantee a null byte at the end of the data. 
        /// </summary>
        VT_BLOB = 65,
        /// <summary>
        /// A blob member containing a serialized object in the same representation that would appear in VT_STREAMED_OBJECT. 
        /// That is, a DWORD byte count (where the byte count does not include the size of itself) which is in the 
        /// format of a class identifier followed by initialization data for that class. 
        /// The only significant difference between VT_BLOB_OBJECT and VT_STREAMED_OBJECT is that the former does not 
        /// have the system-level storage overhead that the latter would have, and is therefore more 
        /// suitable for scenarios involving numbers of small objects.
        /// </summary>
        VT_BLOBOBJECT = 70,
        /// <summary>
        /// pStream Pointer to an IStream interface, representing a stream which is a sibling to the "Contents" stream. 
        /// </summary>
        VT_STREAM = 66,
        /// <summary>
        /// pStream As in VT_STREAM, but indicates that the stream contains a serialized object, which is a CLSID followed by initialization data for the class. The stream is a sibling to the "Contents" stream that contains the property set. 
        /// </summary>
        VT_STREAMED_OBJECT = 68,
        /// <summary>
        /// pStorage Pointer to an IStorage interface, representing a storage object that is a sibling to the "Contents" stream. 
        /// </summary>
        VT_STORAGE = 67,
        /// <summary>
        /// pStorage As in VT_STORAGE, but indicates that the designated IStorage contains a loadable object. 
        /// </summary>
        VT_STORED_OBJECT = 69,
        /// <summary>
        /// decVal A DECIMAL structure. 
        /// </summary>
        VT_DECIMAL = 14,
        /// <summary>
        /// ca* If the type indicator is combined with VT_VECTOR by using an OR operator, the value is 
        /// one of the counted array values. This creates a DWORD count of elements, followed by a 
        /// pointer to the specified repetitions of the value. 
        /// For example, a type indicator of VT_LPSTR|VT_VECTOR has a DWORD element count, 
        /// followed by a pointer to an array of LPSTR elements.
        /// VT_VECTOR can be combined by an OR operator with the following types: 
        /// VT_I1, VT_UI1, VT_I2, VT_UI2, VT_BOOL, VT_I4, VT_UI4, VT_R4, VT_R8, VT_ERROR, VT_I8, 
        /// VT_UI8, VT_CY, VT_DATE, VT_FILETIME, VT_CLSID, VT_CF, VT_BSTR, VT_LPSTR, VT_LPWSTR, and VT_VARIANT.
        /// </summary>
        VT_VECTOR = 0x1000,
        /// <summary>
        /// If the type indicator is combined with VT_ARRAY by an OR operator, the value is 
        /// a pointer to a SAFEARRAY. VT_ARRAY can use the OR with the following data types: 
        /// VT_I1, VT_UI1, VT_I2, VT_UI2, VT_I4, VT_UI4, VT_INT, VT_UINT, VT_R4, VT_R8, VT_BOOL, 
        /// VT_DECIMAL, VT_ERROR, VT_CY, VT_DATE, and VT_BSTR. VT_ARRAY cannot use OR with VT_VECTOR. 
        /// </summary>
        VT_ARRAY = 0x2000,
        /// <summary>
        /// If the type indicator is combined with VT_BYREF by an OR operator, the value is a reference. 
        /// Reference types are interpreted as a reference to data, similar to the 
        /// reference type in C++ (for example, "int&amp;"). 
        /// VT_BYREF can use OR with the following types: VT_I1, VT_UI1, VT_I2, VT_UI2, VT_I4, VT_UI4, 
        /// VT_INT, VT_UINT, VT_R4, VT_R8, VT_BOOL, VT_DECIMAL, VT_ERROR, VT_CY, VT_DATE, 
        /// VT_BSTR, VT_ARRAY, and VT_VARIANT.
        /// </summary>
        VT_BYREF = 0x4000,
        /// <summary>
        /// A DWORD type indicator followed by the corresponding value. VT_VARIANT can be used 
        /// only with VT_VECTOR or VT_BYREF. 
        /// </summary>
        VT_VARIANT = 12,
        /// <summary>
        /// Used as a mask for VT_VECTOR and other modifiers to extract the raw VT value. 
        /// </summary>
        VT_TYPEMASK = 0xFFF
    }

    [Flags]
    internal enum IFILTER_FLAGS : int
    {
        /// <summary>
        /// The caller should use the IPropertySetStorage and IPropertyStorage interfaces to locate additional properties. 
        /// When this flag is set, properties available through COM enumerators should not be returned from IFilter. 
        /// </summary>
        IFILTER_FLAGS_OLE_PROPERTIES = 1
    }

    internal enum IFilterReturnCodes : uint
    {
        /// <summary>
        /// Success
        /// </summary>
        S_OK = 0,
        /// <summary>
        /// The function was denied access to the filter file. 
        /// </summary>
        E_ACCESSDENIED = 0x80070005,
        /// <summary>
        /// The function encountered an invalid handle, probably due to a low-memory situation. 
        /// </summary>
        E_HANDLE = 0x80070006,
        /// <summary>
        /// The function received an invalid parameter.
        /// </summary>
        E_INVALIDARG = 0x80070057,
        /// <summary>
        /// Out of memory
        /// </summary>
        E_OUTOFMEMORY = 0x8007000E,
        /// <summary>
        /// Not implemented
        /// </summary>
        E_NOTIMPL = 0x80004001,
        /// <summary>
        /// Unknown error
        /// </summary>
        E_FAIL = 0x80000008,
        /// <summary>
        /// File not filtered due to password protection
        /// </summary>
        FILTER_E_PASSWORD = 0x8004170B,
        /// <summary>
        /// The document format is not recognised by the filter
        /// </summary>
        FILTER_E_UNKNOWNFORMAT = 0x8004170C,
        /// <summary>
        /// No text in current chunk
        /// </summary>
        FILTER_E_NO_TEXT = 0x80041705,
        /// <summary>
        /// No more chunks of text available in object
        /// </summary>
        FILTER_E_END_OF_CHUNKS = 0x80041700,
        /// <summary>
        /// No more text available in chunk
        /// </summary>
        FILTER_E_NO_MORE_TEXT = 0x80041701,
        /// <summary>
        /// No more property values available in chunk
        /// </summary>
        FILTER_E_NO_MORE_VALUES = 0x80041702,
        /// <summary>
        /// Unable to access object
        /// </summary>
        FILTER_E_ACCESS = 0x80041703,
        /// <summary>
        /// Moniker doesn't cover entire region
        /// </summary>
        FILTER_W_MONIKER_CLIPPED = 0x00041704,
        /// <summary>
        /// Unable to bind IFilter for embedded object
        /// </summary>
        FILTER_E_EMBEDDING_UNAVAILABLE = 0x80041707,
        /// <summary>
        /// Unable to bind IFilter for linked object
        /// </summary>
        FILTER_E_LINK_UNAVAILABLE = 0x80041708,
        /// <summary>
        ///  This is the last text in the current chunk
        /// </summary>
        FILTER_S_LAST_TEXT = 0x00041709,
        /// <summary>
        /// This is the last value in the current chunk
        /// </summary>
        FILTER_S_LAST_VALUES = 0x0004170A
    }
}
