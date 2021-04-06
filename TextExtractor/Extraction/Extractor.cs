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
            Configuration config;
            using (var stream = File.OpenRead("config.json"))
            {
                config = await JsonSerializer.DeserializeAsync<Configuration>(stream, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            // Mapping of languages to the key - value localization strings
            // We could use a ConcurrentDictionary here too, but since we want the keys to stay ordered, we'll use a lock instead
            Dictionary<string, Dictionary<string, string>> values = new Dictionary<string, Dictionary<string, string>>();

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
            var progressUpdateTask = Task.Run(async () => {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    PrintProgress();
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationTokenSource.Token);
                }
            }, cancellationTokenSource.Token);

            // Run extraction for every file
            var tasks = new List<Task>();
            object lockObject = new object();
            foreach(var fileConfig in config.Files)
            {
                progress.TryAdd(fileConfig.Name, new ProgressReport()
                {
                    FileName = fileConfig.Name,
                    LanguageIndex = 0,
                    SectionIndex = 0,
                    SectionCount = fileConfig.Sections.Count
                });

                var extractor = new FileExtractor(fileConfig, values, lockObject);
                extractor.ProgressChanged += UpdateProgress;
                tasks.Add(extractor.ExtractAsync());
            }
            await Task.WhenAll(tasks);

            // Stop updating the progress
            PrintProgress();
            cancellationTokenSource.Cancel();

            // Write the values
            Console.WriteLine("Writing export files...");
            tasks.Clear();
            foreach (var language in values)
            {
                tasks.Add(Task.Run(async () =>
                {
                    string filePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Output", $"{language.Key.ToLower()}.json");
                    string fileContent = JsonSerializer.Serialize(language.Value, new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All),
                        WriteIndented = true
                    });
                    await File.WriteAllTextAsync(filePath, fileContent);
                }));
                
            }
            await Task.WhenAll(tasks);
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
                ClearLine(top);
                Console.WriteLine(progressReport.Value.ToString());
                top++;
            }
        }

        private void ClearLine(int line)
        {
            Console.SetCursorPosition(0, line);
            Console.Write(new string(' ', Console.BufferWidth - Console.CursorLeft));
            Console.SetCursorPosition(0, line);
        }
    }
}
