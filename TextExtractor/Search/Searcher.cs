using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TextExtractor.Search
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

        public async Task SearchFilesAsync(string value)
        {
            string folder = "Files";

            string folderPath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, folder);
            IEnumerable<string> files = Directory.EnumerateFiles(folderPath);
            fileCount = files.Count();
            fileReadCount = 0;

            byte[] valueToSearch = Encoding.Unicode.GetBytes(value);
            Console.WriteLine($"Searching '{value}'...");
            foreach (var file in files)
            {
                await SearchFileAsync(file, valueToSearch);
                fileReadCount++;
                PrintProgress();
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

        private async Task SearchFileAsync(string path, byte[] value)
        {
            int maxBuffer = 1024 * 1024; // 1MB
            this.results[path] = new List<int>();

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, maxBuffer))
            {
                byte[] buffer = new byte[maxBuffer];
                int currentFileOffset = 0;
                int bytesRead = await stream.ReadAsync(buffer);

                while (bytesRead != 0)
                {
                    int stringIndex = Utils.SearchStringInBuffer(buffer, value);
                    if (stringIndex != -1)
                    {
                        this.results[path].Add(currentFileOffset + stringIndex);
                    }

                    currentFileOffset += bytesRead;
                    stream.Seek(currentFileOffset - value.Length, SeekOrigin.Begin);

                    bytesRead = await stream.ReadAsync(buffer);
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
