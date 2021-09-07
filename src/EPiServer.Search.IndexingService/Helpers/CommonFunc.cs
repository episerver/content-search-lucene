using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EPiServer.Search.IndexingService.Helpers
{
    public class CommonFunc : ICommonFunc
    {
        private const IFILTER_INIT FILTERSETTINGS = IFILTER_INIT.IFILTER_INIT_INDEXING_ONLY |
                        IFILTER_INIT.IFILTER_INIT_APPLY_INDEX_ATTRIBUTES |
                        IFILTER_INIT.IFILTER_INIT_APPLY_CRAWL_ATTRIBUTES |
                        IFILTER_INIT.IFILTER_INIT_CANON_SPACES;

        private const Int32 BufferSize = 65536;

        private readonly IResponseExceptionHelper _responseExceptionHelper;
        public CommonFunc(IResponseExceptionHelper responseExceptionHelper)
        {
            _responseExceptionHelper = responseExceptionHelper;
        }
        public bool IsModifyIndex(string namedIndexName)
        {
            if (String.IsNullOrEmpty(namedIndexName))
            {
                namedIndexName = IndexingServiceSettings.DefaultIndexName;
            }

            if (IndexingServiceSettings.NamedIndexElements[namedIndexName].ReadOnly)
            {
                _responseExceptionHelper.HandleServiceError(String.Format("cannot modify index: '{0}'. Index is readonly.", namedIndexName));

                return false;
            }
            else
            {
                return true;
            }
        }

        public string PrepareExpression(string q, bool excludeNotPublished)
        {
            string expression = q;
            expression = PrepareEscapeFields(expression, IndexingServiceSettings.CategoriesFieldName);
            expression = PrepareEscapeFields(expression, IndexingServiceSettings.AclFieldName);
            string currentDate = Regex.Replace(DateTime.Now.ToUniversalTime().ToString("u"), @"\D", "");

            if (excludeNotPublished)
            {
                expression = String.Format("({0}) AND ({1}:(no) OR {1}:[{2} TO 99999999999999])", expression,
                    IndexingServiceSettings.PublicationEndFieldName, currentDate);
            }
            if (excludeNotPublished)
            {
                expression = String.Format("({0}) AND ({1}:(no) OR {1}:[00000000000000 TO {2}])", expression,
                    IndexingServiceSettings.PublicationStartFieldName, currentDate);
            }

            return expression;
        }

        public string PrepareEscapeFields(string q, string fieldName)
        {
            MatchEvaluator regexEscapeFields = delegate (Match m)
            {
                if (m.Groups["fieldname"].Value.Equals(fieldName + ":"))
                {
                    return m.Groups["fieldname"] + "\"" + IndexingServiceSettings.TagsPrefix + m.Groups["terms"].Value.Replace("(", "").Replace(")", "") + IndexingServiceSettings.TagsSuffix + "\"";
                }
                else
                {
                    return m.Groups[0].Value;
                }
            };

            string expr = Regex.Replace(q, "(?<fieldname>\\w+:)?(?:(?<terms>\\([^()]*\\))|(?<terms>[^\\s()\"]+)|(?<terms>\"[^\"]*\"))", regexEscapeFields);

            return expr;
        }

        /// <summary>
        /// Gets whether the supplied named index exists
        /// </summary>
        /// <param name="namedIndexName">the name of the named index</param>
        /// <returns></returns>
        public bool IsValidIndex(string namedIndexName)
        {
            if (String.IsNullOrEmpty(namedIndexName))
            {
                namedIndexName = IndexingServiceSettings.DefaultIndexName;
                if (IndexingServiceSettings.NamedIndexDirectories.ContainsKey(namedIndexName))
                {
                    return true;
                }
            }
            else if (IndexingServiceSettings.NamedIndexDirectories.ContainsKey(namedIndexName))
            {
                return true;
            }
            _responseExceptionHelper.HandleServiceError(String.Format("Named index \"{0}\" is not valid, it does not exist in configuration or has faulty configuration", namedIndexName));
            return false;
        }

        public void SplitDisplayTextToMetadata(string displayText, string metadata, out string displayTextOut, out string metadataOut)
        {
            displayTextOut = String.Empty;
            metadataOut = String.Empty;

            if (displayText.Length <= IndexingServiceSettings.MaxDisplayTextLength)
            {
                displayTextOut = displayText;
                metadataOut = metadata;
                return;
            }
            else
            {
                displayTextOut = displayText.Substring(0, IndexingServiceSettings.MaxDisplayTextLength);
                StringBuilder sb = new StringBuilder();
                sb.Append(metadata); // Add original data
                sb.Append(" ");
                sb.Append(displayText.Substring(IndexingServiceSettings.MaxDisplayTextLength, displayText.Length - IndexingServiceSettings.MaxDisplayTextLength));
                metadataOut = sb.ToString();
            }
        }

        /// <summary>
        /// Gets the text content for the passed uri that is not a file uri. Not implemented. Should be overridden.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> to get content from</param>
        /// <returns>Empty string</returns>
        public virtual string GetNonFileUriContent(Uri uri)
        {
            return "";
        }

        /// <summary>
        /// Gets the text content for the passed file uri using the uri.LocalPath
        /// </summary>
        /// <param name="uri">The file <see cref="Uri"/> to get content from</param>
        /// <returns></returns>     
        public virtual string GetFileUriContent(Uri uri)
        {
            return GetFileText(uri.LocalPath);
        }
        
        public string GetFileText(string path)
        {
            StringBuilder text = new StringBuilder();
            IFilter iflt = null;
            object iunk = null;
            int i = TextFilter.LoadIFilter(path, iunk, ref iflt);
            if (i != (int)IFilterReturnCodes.S_OK)
            {
                return null; //Cannot find a filter for file
            }

            IFilterReturnCodes scode;
            //ArrayList textItems = new ArrayList();

            int attr = 0;
            IFILTER_FLAGS flagsSet = 0;
            scode = iflt.Init(FILTERSETTINGS, attr, IntPtr.Zero, ref flagsSet);
            if (scode != IFilterReturnCodes.S_OK)
            {
                throw new Exception(
                    String.Format("IFilter initialisation failed: {0}",
                    Enum.GetName(scode.GetType(), scode)));
            }

            while (scode == IFilterReturnCodes.S_OK)
            {
                STAT_CHUNK stat = new STAT_CHUNK();

                scode = iflt.GetChunk(ref stat);
                if (scode == IFilterReturnCodes.S_OK)
                {
                    if (stat.flags == CHUNKSTATE.CHUNK_TEXT)
                    {
                        if (text.Length > 0 && stat.breakType != CHUNK_BREAKTYPE.CHUNK_NO_BREAK)
                        {
                            text.AppendLine();
                        }
                        int bufSize = BufferSize;

                        IFilterReturnCodes scodeText = IFilterReturnCodes.S_OK;
                        StringBuilder tmpbuf = new StringBuilder(bufSize);

                        while (scodeText == IFilterReturnCodes.S_OK)
                        {
                            scodeText = iflt.GetText(ref bufSize, tmpbuf);
                            if (scodeText == IFilterReturnCodes.S_OK || scodeText == IFilterReturnCodes.FILTER_S_LAST_TEXT)
                            {
                                if (bufSize > 0)
                                {
                                    text.Append(tmpbuf.ToString(0, (bufSize > tmpbuf.Length ? tmpbuf.Length : bufSize)));
                                }
                            }

                            // We don't need to call again to get FILTER_E_END_OF_CHUNKS
                            if (scodeText == IFilterReturnCodes.FILTER_S_LAST_TEXT)
                            {
                                break;
                            }

                            bufSize = BufferSize;
                        }
                    }
                }
            }

            Marshal.ReleaseComObject(iflt);

            return text.ToString();
        }
    }
}
