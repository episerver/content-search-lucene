using System.Globalization;
using System.Text;

namespace EPiServer.Search.Queries.Lucene
{
    /// <summary>
    /// A Term boost query adds a boost factor to the term that will then have a higher significance when the total search expression is generated
    /// </summary>
    public class TermBoostQuery : FieldQuery
    {
        /// <summary>
        /// Constructs a term boost search query from a word or a phrase, a field and a boost factorr.
        /// </summary>
        /// <param name="phrase">The word or phrase to boost</param>
        /// <param name="field">The field for which this query should apply</param>
        /// <param name="boostFactor">The boost factor to boost the supplied word or phrase</param>
        public TermBoostQuery(string phrase, Field field, float boostFactor)
            : base(phrase, field)
        {
            BoostFactor = boostFactor;
        }

        /// <summary>
        /// Constructs a term boost search query from a word or a phrase, a field and a boost factorr.
        /// </summary>
        /// <param name="phrase">The word or phrase to boost</param>
        /// <param name="boostFactor">The boost factor to boost the supplied word or phrase</param>
        public TermBoostQuery(string phrase, float boostFactor)
            : base(phrase, Field.Default)
        {
            BoostFactor = boostFactor;
        }

        /// <summary>
        /// Gets and sets the boost factor for this <see cref="TermBoostQuery"/>
        /// </summary>
        public float BoostFactor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the text representation of this <see cref="TermBoostQuery"/>
        /// </summary>
        /// <returns></returns>
        public override string GetQueryExpression()
        {
            var sb = new StringBuilder();
            sb.Append(SearchSettings.GetFieldNameForField(Field));
            sb.Append(":(");
            sb.Append(GetSafeQuotedPhrase(Expression));
            sb.Append("^");
            sb.Append(BoostFactor.ToString(CultureInfo.InvariantCulture).Replace(",", "."));
            sb.Append(")");

            return sb.ToString();
        }
    }
}
