namespace FileParser.Core.Extraction;

public interface IExtractionService
{
    void Extract(string inputFolder, string outputFolder, ExtractionCallbacks callbacks);
}
