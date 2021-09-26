using System.Threading;
using System.Threading.Tasks;

namespace FileParser.Extraction
{
    public interface IExtractionService
    {
        Task ExtractAsync(ExtractParameters parameters, CancellationToken token);
    }
}
