using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading.Tasks;
using TextExtractor.Config;
using TextExtractor.Extraction;

namespace TextExtractor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var textExtractor = new Extractor();

            Stopwatch watch = new Stopwatch();
            watch.Start();

            await textExtractor.RunAsync();

            watch.Stop();
            Console.WriteLine($"Finished in {watch.ElapsedMilliseconds / 1000}s !");
            Console.ReadKey();
        }
    }
}
