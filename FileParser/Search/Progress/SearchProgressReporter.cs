using System;

namespace FileParser.Search
{
    public class SearchProgressReporter : ISearchProgressReporter
    {
        public void ReportProgress(int fileRead, int fileCount)
        {
            Utils.ClearLine(1);
            Console.WriteLine($"Read {fileRead} out of {fileCount} files...");
        }
    }
}
