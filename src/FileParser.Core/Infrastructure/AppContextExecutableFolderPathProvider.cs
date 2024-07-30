namespace FileParser.Core.Infrastructure;

internal class AppContextExecutableFolderPathProvider : IExecutableFolderPathProvider
{
    public string ExecutableFolderPath => AppContext.BaseDirectory;
}
