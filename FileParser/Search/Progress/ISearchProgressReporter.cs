namespace FileParser.Search
{
    public interface ISearchProgressReporter
    {
        public void ReportProgress(int fileRead, int fileCount);
    }
}
