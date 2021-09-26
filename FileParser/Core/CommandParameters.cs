namespace FileParser.Core
{
    public abstract class CommandParameters
    {
        /// <summary>
        /// Path to the folder with the input files.
        /// </summary>
        public string Input { get; set; } = string.Empty;

        /// <summary>
        /// Verbose flag.
        /// </summary>
        public bool Verbose { get; set; }
    }
}
