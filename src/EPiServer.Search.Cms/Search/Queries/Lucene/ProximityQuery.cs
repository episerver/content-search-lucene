using System.Globalization;
using System.Text;

namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// A Proximity search query searches for words in the supplied phrase within the Distance
    /// </summary>
    public class ProximityQuery : FieldQuery
    {
        /// <summary>
        /// Constructs a Fuzzy search query from a phrase, a field and a distance.
        /// </summary>
        /// <param name="phrase">The phrase containing the words with the maximum distance from each other</param>
        /// <param name="field">The field for which this query should apply.</param>
        /// <param name="distance">The maximum number of words allowed between the words in the phrase to result in a match</param>
        public ProximityQuery(string phrase, Field field, int distance)
            : base(phrase, field)
        {
            base.Expression = Expression;
            Distance = distance;
        }

        /// <summary>
        /// Constructs a Fuzzy search query from a phrase, a field and a distance.
        /// </summary>
        /// <param name="phrase">The phrase containing the words with the maximum distance from each other</param>
        /// <param name="distance">The maximum number of words allowed between the words in the phrase to result in a match</param>
        public ProximityQuery(string phrase, int distance)
            : base(phrase, Field.Default)
        {
            base.Expression = Expression;
            Distance = distance;
        }

        /// <summary>
        /// Gets and sets the distance which is the maximum number of words that may be between the words in the phrase
        /// </summary>
        public int Distance
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the text representation of this <see cref="ProximityQuery"/>
        /// </summary>
        /// <returns></returns>
        public override string GetQueryExpression()
        {
            var sb = new StringBuilder();
            sb.Append(SearchSettings.GetFieldNameForField(Field));
            sb.Append(":(");
            sb.Append(GetSafeQuotedPhrase(Expression));
            sb.Append("~");
            sb.Append(Distance.ToString(CultureInfo.InvariantCulture));
            sb.Append(")");

            return sb.ToString();
        }
    }
}
