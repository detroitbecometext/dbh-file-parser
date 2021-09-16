using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using FileParser.Config;

namespace FileParser.Extraction
{
    public class Extractor
    {
        private readonly ConcurrentDictionary<string, ProgressReport> progress = new();
        private readonly SemaphoreSlim semaphore = new(1, 1);

        public async Task RunAsync(ExtractParameters parameters, CancellationToken token)
        {
            Configuration config;
            using (var stream = File.OpenRead("config.json"))
            {
                config = await JsonSerializer.DeserializeAsync<Configuration>(stream, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                }, token).ConfigureAwait(false) ?? throw new Exception("Couldn't load the configuration file.");
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

            // Update the extraction process every 200ms
            var cancellationTokenSource = new CancellationTokenSource();
            if (parameters.Verbose)
            {
                var progressUpdateTask = Task.Run(async () => {
                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        PrintProgress();
                        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }, cancellationTokenSource.Token);
            }

            // Run extraction for every file
            var tasks = new List<Task>();
            foreach(var fileConfig in config.Files)
            {
                progress.TryAdd(fileConfig.Name, new ProgressReport()
                {
                    FileName = fileConfig.Name,
                    LanguageIndex = 0,
                    SectionIndex = 0,
                    SectionCount = fileConfig.Sections.Count
                });

                var extractor = new FileExtractor(fileConfig, values, semaphore);
                extractor.ProgressChanged += UpdateProgress;
                tasks.Add(extractor.ExtractAsync(parameters, token));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Stop updating the progress
            PrintProgress();
            cancellationTokenSource.Cancel();

            // Write the values
            if (parameters.Verbose)
            {
                Console.WriteLine("Writing export files...");
            }
            tasks.Clear();

            var folderPath = Utils.GetAbsoluteFolderPath(parameters.Output);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            foreach (var language in values)
            {
                tasks.Add(Task.Run(async () =>
                {
                    string filePath = Utils.GetAbsoluteFolderPath(folderPath, $"{language.Key.ToLower()}.json");
                    string fileContent = JsonSerializer.Serialize(language.Value, new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All)
                    });
                    await File.WriteAllTextAsync(filePath, fileContent).ConfigureAwait(false);
                }, token));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private void UpdateProgress(object? sender, ProgressReport e)
        {
            this.progress[e.FileName] = e;
        }

        private void PrintProgress()
        {
            int top = 0;

            foreach (var progressReport in progress.OrderBy(p => p.Key))
            {
                Utils.ClearLine(top);
                Console.WriteLine(progressReport.Value.ToString());
                top++;
            }
        }
    }
}
