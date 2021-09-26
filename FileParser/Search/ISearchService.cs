using System.Threading;
using System.Threading.Tasks;

namespace FileParser.Search
{
    public interface ISearchService
    {
        Task SearchAsync(SearchParameters parameters, CancellationToken token);
    }
}
