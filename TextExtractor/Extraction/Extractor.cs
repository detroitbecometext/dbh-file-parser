using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using TextExtractor.Config;

namespace TextExtractor.Extraction
{
    public class Extractor
    {
        private readonly ConcurrentDictionary<string, ProgressReport> progress = new ConcurrentDictionary<string, ProgressReport>();

        public async Task RunAsync()
        {
            // Mapping of languages to the key - value localization strings
            ConcurrentDictionary<string, ConcurrentDictionary<string, string>> values = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

            // Languages will always appear in this order
            foreach (var lang in Configuration.Languages)
            {
                values.TryAdd(lang, new ConcurrentDictionary<string, string>());
            }

            ConcurrentDictionary<string, ProgressReport> progress = new ConcurrentDictionary<string, ProgressReport>();

            Configuration config;
            using (var stream = File.OpenRead("config.json"))
            {
                config = await JsonSerializer.DeserializeAsync<Configuration>(stream, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            // Update the extraction process every 200ms
            var cancellationTokenSource = new CancellationTokenSource();
            var progressUpdateTask = Task.Run(async () => {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    PrintProgress();
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationTokenSource.Token);
                }
            }, cancellationTokenSource.Token);

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

                var extractor = new FileExtractor();
                extractor.ProgressChanged += UpdateProgress;
                tasks.Add(extractor.ExtractAsync(fileConfig, values));
            }
            await Task.WhenAll(tasks);

            // Stop updating the progress
            cancellationTokenSource.Cancel();

            // Write the values
            Console.WriteLine("Writing export files...");
            foreach (var language in values)
            {
                string filePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Output", $"{language.Key.ToLower()}.json");
                string fileContent = JsonSerializer.Serialize(language.Value, new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All),
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(filePath, fileContent);
            }
        }

        private void UpdateProgress(object sender, ProgressReport e)
        {
            this.progress[e.FileName] = e;
        }

        private void PrintProgress()
        {
            int top = 0;

            foreach (var progressReport in progress)
            {
                // Clear line
                Console.SetCursorPosition(0, top);
                Console.Write(new string(' ', Console.BufferWidth - Console.CursorLeft));

                // Print progress
                Console.SetCursorPosition(0, top);
                Console.WriteLine(progressReport.Value.ToString());
                top++;
            }
        }
    }
}
