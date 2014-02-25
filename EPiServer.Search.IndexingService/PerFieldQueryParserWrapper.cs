using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace EPiServer.Search.IndexingService
{
    /// <summary>
    /// Wrapper for QueryParser which uses SetLowercaseExpandedTerms selectively for wilcard/range queries 
    /// to match the varying case sensitivity behavior you can have with PerFieldAnalyzerWrapper
    /// </summary>
    internal class PerFieldQueryParserWrapper : QueryParser
     {
        private IList<string> _lowercaseFields;

        public PerFieldQueryParserWrapper(Version matchVersion, System.String f, Analyzer a, IList<string> lowercaseFields)
            : base(matchVersion, f, a)
        {
            _lowercaseFields = lowercaseFields;
        }

        protected override Query GetWildcardQuery(string field, string termStr)
        {
            try
            {
                if (!_lowercaseFields.Contains(field))
                {
                    LowercaseExpandedTerms = false;
                }

                return base.GetWildcardQuery(field, termStr);
            }
            finally
            {
                if (!_lowercaseFields.Contains(field))
                {
                    LowercaseExpandedTerms = true;
                }
            }
        }

        protected override Query GetPrefixQuery(string field, string termStr)
        {
            try
            {
                if (!_lowercaseFields.Contains(field))
                {
                    LowercaseExpandedTerms = false;
                }

                return base.GetPrefixQuery(field, termStr);
            }
            finally
            {
                if (!_lowercaseFields.Contains(field))
                {
                    LowercaseExpandedTerms = true;
                }
            }
        }

        protected override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
        {
            try
            {
                if (!_lowercaseFields.Contains(field))
                {
                    LowercaseExpandedTerms = false;
                }

                return base.GetFuzzyQuery(field, termStr, minSimilarity);
            }
            finally
            {
                if (!_lowercaseFields.Contains(field))
                {
                    LowercaseExpandedTerms = true;
                }
            }
        }

        protected override Query GetRangeQuery(string field, string part1, string part2, bool inclusive)
        {
            try
            {
                if (!_lowercaseFields.Contains(field))
                {
                    LowercaseExpandedTerms = false;
                }

                return base.GetRangeQuery(field, part1, part2, inclusive);
            }
            finally
            {
                if (!_lowercaseFields.Contains(field))
                {
                    LowercaseExpandedTerms = true;
                }
            }
        }
    }
}