namespace FileParser.Core
{
    public interface ICommandLineArgumentsProvider
    {
        public string[] Arguments { get; }
        public string ExecutableFolderPath { get; }
    }
}
