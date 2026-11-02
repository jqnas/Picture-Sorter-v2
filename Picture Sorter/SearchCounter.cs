namespace Picture_Sorter
{
    /// <summary>
    /// Provides search result counts.
    /// </summary>
    public sealed class SearchCounter
    {
        /// <summary>
        /// The count of all search results.
        /// </summary>
        public int TotalResults { get; set; }

        /// <summary>
        /// The current result number.
        /// </summary>
        public int CurrentResultNumber { get; set; }


        /// <summary>
        /// Creates a new <see cref="SearchCounter"/>.
        /// </summary>
        /// <param name="totalResults">The total count of search results.</param>
        /// <param name="currentResult">The current result number.</param>
        public SearchCounter(int totalResults, int currentResult)
        {
            TotalResults = totalResults;
            CurrentResultNumber = currentResult;
        }
    }
}
