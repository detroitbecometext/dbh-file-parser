using FileParser.Core;
using FileParser.Extraction;
using FileParser.Search;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace FileParser
{
    public static class Program
    {
        private static async Task Main(string[] args)
        {
            await Host
                .CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<IFileSystem, FileSystem>();
                    services.AddSingleton<ICommandLineArgumentsProvider, CommandLineArgumentsProvider>();
                    services.AddSingleton<IConsoleWriter, ConsoleWriter>();

                    services.AddSingleton<IExtractionService, ExtractionService>();
                    services.AddSingleton<IExtractionProgressReporter, ExtractionProgressReporter>();

                    services.AddSingleton<ISearchService, SearchService>();
                    services.AddSingleton<ISearchProgressReporter, SearchProgressReporter>();

                    services.AddHostedService<HostedService>();
                })
                .ConfigureLogging((_, config) => config.ClearProviders())
                .RunConsoleAsync()
                .ConfigureAwait(false);
        }
    }
}
