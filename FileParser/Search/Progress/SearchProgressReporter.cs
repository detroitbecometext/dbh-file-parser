using FileParser.Core;
using System;

namespace FileParser.Search
{
    public class SearchProgressReporter : ISearchProgressReporter
    {
        private readonly IConsoleWriter consoleWriter;

        public SearchProgressReporter(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void ReportProgress(int fileRead, int fileCount)
        {
            consoleWriter.ClearLine(1);
            consoleWriter.WriteLine($"Read {fileRead} out of {fileCount} files...");
        }
    }
}
