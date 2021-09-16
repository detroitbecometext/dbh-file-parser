using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileParser.Search
{
    public class Searcher
    {
        private readonly ConcurrentDictionary<string, List<int>> results;
        private int fileCount;
        private int fileReadCount;

        public Searcher()
        {
            results = new ConcurrentDictionary<string, List<int>>();
        }

        public async Task SearchFilesAsync(SearchParameters parameters, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(parameters.Value))
            {
                throw new ArgumentException("The search value shouldn't be empty.");
            }

            var folderPath = Utils.GetAbsoluteFolderPath(parameters.Input);
            if (!Directory.Exists(folderPath))
            {
                throw new ArgumentException($"Cannot find the folder at path {folderPath}.");
            }

            IEnumerable<string> files = Directory.EnumerateFiles(folderPath);
            fileCount = files.Count();
            fileReadCount = 0;

            // Keys are in UTF-8, translation values are in UTF-16 (Unicode)
            byte[] valueToSearch = parameters.InKeys ? Encoding.UTF8.GetBytes(parameters.Value) : Encoding.Unicode.GetBytes(parameters.Value);

            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();

                await SearchFileAsync(file, valueToSearch, parameters.BufferSize, token).ConfigureAwait(false);

                if(parameters.Verbose && !Console.IsOutputRedirected)
                {
                    fileReadCount++;
                    PrintProgress();
                }
            }

            if(results.All(r => r.Value.Count == 0))
            {
                Console.WriteLine("Value not found.");
            }
            else
            {
                foreach (var fileResult in results.Where(r => r.Value.Count > 0))
                {
                    Console.WriteLine($"Found in {fileResult.Key} at offsets [{string.Join(", ", fileResult.Value)}]");
                }
            }
        }

        private async Task SearchFileAsync(string path, byte[] value, int bufferSize, CancellationToken token)
        {
            this.results[path] = new List<int>();

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
            {
                byte[] buffer = new byte[bufferSize];
                int currentFileOffset = 0;
                int bytesRead = await stream.ReadAsync(buffer, token).ConfigureAwait(false);

                while (bytesRead != 0)
                {
                    token.ThrowIfCancellationRequested();

                    int stringIndex = Utils.SearchStringInBuffer(buffer, value);
                    if (stringIndex != -1)
                    {
                        this.results[path].Add(currentFileOffset + stringIndex);
                    }

                    currentFileOffset += bytesRead;
                    stream.Seek(currentFileOffset - value.Length, SeekOrigin.Begin);

                    bytesRead = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
                }
            }
        }

        private void PrintProgress()
        {
            Utils.ClearLine(1);
            Console.WriteLine($"Read {fileReadCount} out of {fileCount} files...");
        }
    }
}
