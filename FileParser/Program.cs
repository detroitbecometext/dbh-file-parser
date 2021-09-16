using FileParser.Extraction;
using FileParser.Search;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FileParser
{
    public static class Program
    {
        private static async Task Main(string[] args)
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
            extractCommand.Handler = CommandHandler.Create<ExtractParameters, CancellationToken>(ExtractAsync);

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
            searchCommand.Handler = CommandHandler.Create<SearchParameters, CancellationToken>(SearchAsync);

            var rootCommand = new RootCommand()
            {
                extractCommand,
                searchCommand
            };

            await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        private static async Task ExtractAsync(ExtractParameters parameters, CancellationToken token)
        {
            var textExtractor = new Extractor();

            var watch = new Stopwatch();
            watch.Start();

            if (!Console.IsOutputRedirected)
            {
                Console.WriteLine("Extracting...");
            }

            try
            {
                await textExtractor.RunAsync(parameters, token).ConfigureAwait(false);
            }
            catch(OperationCanceledException)
            {
                Console.Clear();
                Console.WriteLine("Stopping.");
                return;
            }

            watch.Stop();
            Console.WriteLine($"Finished in {watch.ElapsedMilliseconds / 1000}s.");
        }

        private static async Task SearchAsync(SearchParameters parameters, CancellationToken token)
        {
            var searcher = new Searcher();

            var watch = new Stopwatch();
            watch.Start();

            if(!Console.IsOutputRedirected)
            {
                Console.WriteLine($"Searching '{parameters.Value}' in translation {(parameters.InKeys ? "keys" : "values")}...");
            }

            try
            {
                await searcher.SearchFilesAsync(parameters, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Console.Clear();
                Console.WriteLine("Stopping.");
                return;
            }

            watch.Stop();
            Console.WriteLine($"Finished in {watch.ElapsedMilliseconds / 1000}s.");
        }
    }
}
