using FileParser.Core;
using FileParser.Extraction;
using FileParser.Search;
using Microsoft.Extensions.Hosting;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileParser
{
    public sealed class HostedService : IHostedService
    {
        private int? exitCode = null;

        private readonly RootCommand rootCommand;
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly ICommandLineArgumentsProvider commandLineArgumentsProvider;
        private readonly IExtractionService extractionService;
        private readonly ISearchService searchService;

        public HostedService(
            IHostApplicationLifetime applicationLifetime,
            ICommandLineArgumentsProvider commandLineArgumentsProvider,
            IExtractionService extractionService,
            ISearchService searchService)
        {
            this.applicationLifetime = applicationLifetime;
            this.commandLineArgumentsProvider = commandLineArgumentsProvider;
            this.extractionService = extractionService;
            this.searchService = searchService;
            this.rootCommand = CreateRootCommand();
        }

        private RootCommand CreateRootCommand()
        {
            var inputOption = new Option<string>(
                    new string[] { "--input", "-i" },
                    "./input",
                    description: "Path to the folder with the input files. Will default to folder called 'input' in the current folder.");

            var outputOption = new Option<string>(
                    new string[] { "--output", "-o" },
                    "./output",
                    description: "Path to the output folder. Will default to a folder called 'output' in the current folder.");

            var verboseOption = new Option<bool>(
                    new string[] { "--verbose", "-v" },
                    false,
                    description: "Show execution progress.");

            var bufferOption = new Option<int>(
                    new string[] { "--buffer-size", "-b" },
                    104857600,
                    description: "Size for the buffer used to process files, in bytes. Will default to 100 Mb.");

            var inkeysOption = new Option<bool>(
                    new string[] { "--in-keys", "-k" },
                    false,
                    "Search in translation keys instead of translation values.");

            var extractCommand = new Command("extract")
            {
                 inputOption,
                 outputOption,
                 verboseOption
            };
            extractCommand.Handler = CommandHandler.Create<ExtractParameters, CancellationToken>(extractionService.ExtractAsync);

            var searchCommand = new Command("search")
            {
                new Argument<string>("value")
                {
                    Description = "The value to search (case sensitive)."
                },
                inputOption,
                verboseOption,
                bufferOption,
                inkeysOption
            };
            searchCommand.Handler = CommandHandler.Create<SearchParameters, CancellationToken>(searchService.SearchAsync);

            return new RootCommand()
            {
                extractCommand,
                searchCommand
            };
        }

        async Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await rootCommand.InvokeAsync(commandLineArgumentsProvider.Arguments).ConfigureAwait(false);
                exitCode = 0;
            }
            catch (OperationCanceledException)
            {
                exitCode = 1;
                Console.WriteLine("Stopping.");
                return;
            }
            catch (FileNotFoundException e)
            {
                exitCode = 1;
                Utils.WriteErrorLine($"Cannot find the file '{e.FileName}'.");
                return;
            }
            catch(Exception e)
            {
                exitCode = 1;
                Utils.WriteErrorLine(e.Message);
            }
            finally
            {
                applicationLifetime.StopApplication();
            }
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            Environment.ExitCode = exitCode ?? -1;
            return Task.CompletedTask;
        }
    }
}
