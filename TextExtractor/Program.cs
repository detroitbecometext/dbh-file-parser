using System;
using System.Diagnostics;
using System.Linq;
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

            switch(args[0])
            {
                case "extract":
                    await ExtractAsync(args[1..]);
                    break;
                case "search":
                    bool inKeys = false;
                    if(args.Any(a => a == "-k"))
                    {
                        inKeys = true;
                        args = args.Where(a => a != "-k").ToArray();
                    }

                    if(args.Length != 2)
                    {
                        Console.WriteLine("Please enter one search term.");
                    }
                    else
                    {
                        await SearchAsync(args[1], inKeys);
                    }
                    break;
                default:
                    Console.WriteLine("Please enter a valid command.");
                    break;
            }

            Console.ReadKey();
        }

        private static async Task ExtractAsync(string[] files)
        {
            var textExtractor = new Extractor();

            Stopwatch watch = new Stopwatch();
            watch.Start();

            await textExtractor.RunAsync(files);

            watch.Stop();
            Console.WriteLine($"Finished in {watch.ElapsedMilliseconds / 1000}s !");
        }

        private static async Task SearchAsync(string value, bool inKeys)
        {
            var searcher = new Searcher();

            Stopwatch watch = new Stopwatch();
            watch.Start();

            await searcher.SearchFilesAsync(value, inKeys);

            watch.Stop();
            Console.WriteLine($"Finished in {watch.ElapsedMilliseconds / 1000}s !");
        }
    }
}
