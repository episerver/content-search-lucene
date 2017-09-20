using System.Globalization;
using System.Text;
using System;

namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// A Fuzzy search query can match similar words to the supplied word using a similarity factor.
    /// </summary>
    public class FuzzyQuery : FieldQuery
    {
        /// <summary>
        /// Constructs a Fuzzy search query from a single word, a field and a similarity factor.
        /// </summary>
        /// <param name="word">The word to find similar words for</param>
        /// <param name="field">The field for which this query should apply</param>
        /// <param name="similarityFactor">The similarity between 0 and 1 where values closer to 1 will match words with a higher similarity</param>
        public FuzzyQuery(string word, Field field, float similarityFactor)
            : base(word, field)
        {
            ValidateSimilarityFactor(similarityFactor, "similarityFactor");
            SimilarityFactor = similarityFactor;
        }

        /// <summary>
        /// Constructs a Fuzzy search query from a single word, a field and a similarity factor.
        /// </summary>
        /// <param name="word">The word to find similar words for</param>
        /// <param name="similarityFactor">The similarity between 0 and 1 where values closer to 1 will match words with a higher similarity</param>
        public FuzzyQuery(string word, float similarityFactor)
            : base(word, Field.Default)
        {
            ValidateSimilarityFactor(similarityFactor, "similarityFactor");
            SimilarityFactor = similarityFactor;
        }

        /// <summary>
        /// Gets and sets the similarity between 0 and 1 where values closer to 1 will match words with a higher similarity
        /// </summary>
        public float SimilarityFactor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the text representation of this <see cref="FuzzyQuery"/>
        /// </summary>
        /// <returns></returns>
        public override string GetQueryExpression()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(SearchSettings.GetFieldNameForField(Field));
            sb.Append(":(");
            sb.Append(LuceneHelpers.Escape(Expression));
            sb.Append("~");
            sb.Append(SimilarityFactor.ToString(CultureInfo.InvariantCulture).Replace(",", "."));
            sb.Append(")");

            return sb.ToString();
        }

        private void ValidateSimilarityFactor(float similarityFactor, string argumentName)
        {
            if (similarityFactor < 0f || similarityFactor > 1f)
            {
                throw new ArgumentException("The similarity factor must be between 0 and 1", argumentName);
            }
        }
    }
}
