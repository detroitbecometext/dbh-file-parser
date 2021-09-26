using FileParser.Config;

namespace FileParser.Extraction
{
    public interface IExtractionProgressReporter
    {
        /// <summary>
        /// Start printing the extraction progress.
        /// </summary>
        void StartPrinting();

        /// <summary>
        /// Stop printing the extraction progress.
        /// </summary>
        void StopPrinting();

        /// <summary>
        /// Add a file extractor to the progress tracker.
        /// </summary>
        /// <param name="fileConfig">The configuration for the extracted file.</param>
        /// <param name="extractor">The extractor for the file.</param>
        void AddFileExtractor(FileConfig fileConfig, FileExtractor extractor);
    }
}
