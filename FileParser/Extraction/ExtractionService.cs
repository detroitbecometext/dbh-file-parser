using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using FileParser.Config;

namespace FileParser.Extraction
{
    /// <summary>
    /// Service to extract the game files content and write it to json.
    /// </summary>
    public class ExtractionService : IExtractionService
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);
        private readonly IExtractionProgressReporter progressReporter;
        private readonly IFileSystem fileSystem;

        public ExtractionService(IExtractionProgressReporter progressReporter, IFileSystem fileSystem)
        {
            this.progressReporter = progressReporter;
            this.fileSystem = fileSystem;
        }

        public async Task ExtractAsync(ExtractParameters parameters, CancellationToken token)
        {
            Stopwatch? watch = null;

            if (parameters.Verbose)
            {
                watch = new Stopwatch();
                watch.Start();

                Console.WriteLine("Extracting...");
            }

            await RunExtractionAsync(parameters, token).ConfigureAwait(false);

            if (parameters.Verbose && watch != null)
            {
                watch.Stop();
                Console.WriteLine($"Finished in {watch.ElapsedMilliseconds / 1000}s.");
            }
        }

        private async Task RunExtractionAsync(ExtractParameters parameters, CancellationToken token)
        {
            string configFilePath = fileSystem.GetAbsolutePath(fileSystem.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "config.json");

            if (!fileSystem.File.Exists(configFilePath))
            {
                throw new FileNotFoundException($"Cannot find the configuration file at '{configFilePath}'.", "config.json");
            }

            Configuration config;
            using (var stream = fileSystem.File.OpenRead(configFilePath))
            {
                config = await JsonSerializer.DeserializeAsync<Configuration>(stream, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                }, token).ConfigureAwait(false) ?? throw new JsonException("Couldn't load the configuration file.");
            }

            // Mapping of languages to the <key, value> localization strings
            // We could use a ConcurrentDictionary here too, but since we want the keys to stay ordered, we'll use a semaphore instead
            Dictionary<string, Dictionary<string, string>> values = new();

            var localizationKeys = new Dictionary<string, string>();
            foreach (var key in config.Files.SelectMany(f => f.Sections).SelectMany(s => s.Keys))
            {
                localizationKeys[key] = string.Empty;
            }

            // Languages will always appear in this order
            foreach (var lang in Configuration.Languages)
            {
                values.Add(lang, new Dictionary<string, string>(localizationKeys));
            }

            if (parameters.Verbose)
            {
                progressReporter.StartPrinting();
            }

            // Run extraction for every file
            var tasks = new List<Task>();
            foreach(var fileConfig in config.Files)
            {
                var extractor = new FileExtractor(fileConfig, values, semaphore, fileSystem);
                progressReporter.AddFileExtractor(fileConfig, extractor);
                tasks.Add(extractor.ExtractAsync(parameters, token));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Stop updating the progress
            progressReporter.StopPrinting();

            // Write the values
            if (parameters.Verbose)
            {
                Console.WriteLine("Writing export files...");
            }
            tasks.Clear();

            var folderPath = fileSystem.GetAbsolutePath(parameters.Output);
            if (!fileSystem.Directory.Exists(folderPath))
            {
                fileSystem.Directory.CreateDirectory(folderPath);
            }

            foreach (var language in values)
            {
                tasks.Add(Task.Run(async () =>
                {
                    string filePath = fileSystem.GetAbsolutePath(folderPath, $"{language.Key.ToLower()}.json");
                    string fileContent = JsonSerializer.Serialize(language.Value, new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All)
                    });
                    await fileSystem.File.WriteAllTextAsync(filePath, fileContent).ConfigureAwait(false);
                }, token));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
