using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TextExtractor.Extraction;
using TextExtractor.Search;

namespace TextExtractor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if(args.Length == 0)
            {
                args = new string[] { "extract" };
            }

            if(args[0] == "extract")
            {
                await ExtractAsync(args[1..]);
            }
            else
            {
                string value = args[1];
                await SearchAsync(value);
            }
        }

        private static async Task ExtractAsync(string[] files)
        {
            var textExtractor = new Extractor();

            Stopwatch watch = new Stopwatch();
            watch.Start();

            await textExtractor.RunAsync(files);

            watch.Stop();
            Console.WriteLine($"Finished in {watch.ElapsedMilliseconds / 1000}s !");
            Console.ReadKey();
        }

        private static async Task SearchAsync(string value)
        {
            var searcher = new Searcher();

            Stopwatch watch = new Stopwatch();
            watch.Start();

            await searcher.SearchFilesAsync(value);

            watch.Stop();
            Console.WriteLine($"Finished in {watch.ElapsedMilliseconds / 1000}s !");
            Console.ReadKey();
        }
    }
}
