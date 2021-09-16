namespace FileParser.Search
{
    public class SearchParameters : CommandParameters
    {
        /// <summary>
        /// Value to search.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Search in keys flag.
        /// </summary>
        public bool InKeys { get; set; }

        /// <summary>
        /// Size of the buffer to use.
        /// </summary>
        public int BufferSize { get; set; }
    }
}
