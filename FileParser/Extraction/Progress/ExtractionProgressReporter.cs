using FileParser.Config;
using FileParser.Core;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileParser.Extraction
{
    public class ExtractionProgressReporter : IExtractionProgressReporter
    {
        private readonly ConcurrentDictionary<string, ProgressReport> progressReports = new();
        private readonly CancellationTokenSource cancellationTokenSource = new();

        private readonly IConsoleWriter consoleWriter;

        public ExtractionProgressReporter(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public void StartPrinting()
        {
            // Update the extraction process every 200ms
            var progressUpdateTask = Task.Run(async () => {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    PrintProgress();
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }, cancellationTokenSource.Token);
        }

        public void StopPrinting()
        {
            this.cancellationTokenSource.Cancel();
        }

        public void AddFileExtractor(FileConfig fileConfig, FileExtractor extractor)
        {
            progressReports.TryAdd(fileConfig.Name, new ProgressReport()
            {
                FileName = fileConfig.Name,
                LanguageIndex = 0,
                SectionIndex = 0,
                SectionCount = fileConfig.Sections.Count
            });

            extractor.ProgressChanged += (_, progressReport) => this.progressReports[progressReport.FileName] = progressReport;
        }

        private void PrintProgress()
        {
            int currentLine = 0;

            foreach (var progressReport in progressReports.OrderBy(p => p.Key))
            {
                consoleWriter.ClearLine(currentLine);
                consoleWriter.WriteLine(progressReport.Value.ToString());
                currentLine++;
            }
        }
    }
}
