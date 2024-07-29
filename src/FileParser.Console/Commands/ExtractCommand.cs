using FileParser.Core.Extraction;
using FileParser.Core.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.IO.Abstractions;
using FileParser.Core.Extensions;

namespace FileParser.Console.Commands;

internal sealed class ExtractCommand : Command<ExtractCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("The path to the folder containing the BigFile_PC.d* files. Defaults to the current directory.")]
        [CommandOption("-i|--input")]
        public string? InputPath { get; init; }

        [Description("The path to the folder where the output files will be saved. Defaults to 'output'. The folder will be created if it does not exists.")]
        [CommandOption("-o|--output")]
        public string? OutputPath { get; init; }

        [Description("If specified, display warnings and error stack traces.")]
        [CommandOption("-v|--verbose")]
        [DefaultValue(false)]
        public bool Verbose { get; init; }
    }

    private readonly IFileSystem fileSystem;
    private readonly IExtractionService extractionService;
    private readonly IAnsiConsole console;
    private readonly IExecutableFolderPathProvider executableFolderPathProvider;

    public ExtractCommand(IFileSystem fileSystem, IAnsiConsole console, IExtractionService extractionService, IExecutableFolderPathProvider executableFolderPathProvider)
    {
        this.fileSystem = fileSystem;
        this.console = console;
        this.extractionService = extractionService;
        this.executableFolderPathProvider = executableFolderPathProvider;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        string inputPath = fileSystem.GetAbsolutePath(string.IsNullOrWhiteSpace(settings.InputPath) ? executableFolderPathProvider.ExecutableFolderPath : settings.InputPath);
        string outputPath = fileSystem.GetAbsolutePath(string.IsNullOrWhiteSpace(settings.OutputPath) ? fileSystem.Path.Combine(executableFolderPathProvider.ExecutableFolderPath, "output") : settings.OutputPath);

        console.MarkupLine($"[bold]Input path:[/] {inputPath}");
        console.MarkupLine($"[bold]Output path:[/] {outputPath}");

        return console
            .Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(), 
                new RemainingTimeColumn(), 
                new SpinnerColumn())
            .Start<int>(ctx =>
            {
                var getBuffersTask = ctx.AddTask("Get localization buffers");
                var processBuffersTask = ctx.AddTask("Process buffers");
                var writeFilesTask = ctx.AddTask("Write output files");

                ExtractionCallbacks callbacks = new()
                {
                    OnBuffersDone = (int buffersCount) =>
                    {
                        getBuffersTask.Value = 100;
                        getBuffersTask.StopTask();
                        processBuffersTask.MaxValue = buffersCount;
                    },
                    OnBufferProcessed = () =>
                    {
                        processBuffersTask.Increment(1);
                    },
                    OnFileWriteStarted = (int filesCount) =>
                    {
                        writeFilesTask.MaxValue = filesCount;
                    },
                    OnFileWritten = () =>
                    {
                        writeFilesTask.Increment(1);
                    },
                    OnError = (string message, Exception exception) =>
                    {
                        console.MarkupLine($"[red]{message}[/]");

                        if(settings.Verbose)
                        {
                            console.WriteException(exception, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes | ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
                        }
                    },
                    OnWarning = (string message) =>
                    {
                        if (settings.Verbose)
                        {
                            console.MarkupLine($"[yellow]{message}[/]");
                        }
                    }
                };

                try
                {
                    extractionService.Extract(inputPath, outputPath, callbacks);
                }
                catch(Exception ex)
                {
                    callbacks.OnError($"An unexpected error occurred: {ex.Message}", ex);
                }

                return 0;
            });
    }
}
