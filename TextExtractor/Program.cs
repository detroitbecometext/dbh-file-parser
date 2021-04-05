using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using TextExtractor.Config;

namespace TextExtractor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Mapping of languages to the key - value localization strings
            Dictionary<string, Dictionary<string, string>> values = new Dictionary<string, Dictionary<string, string>>();

            // Languages will always appear in this order
            foreach(var lang in Configuration.Languages){
                values.Add(lang, new Dictionary<string, string>());
            }

            Configuration config;
            using(var stream = File.OpenRead("config.json"))
            {
                config = await JsonSerializer.DeserializeAsync<Configuration>(stream, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            foreach (var fileConfig in config.Files)
            {
                Console.WriteLine($"Exporting file '{fileConfig.Name}'...");

                // TODO: temp
                if(fileConfig.Name == "BigFile_PC.dat")
                {
                    continue;
                }

                int maxBuffer = fileConfig.Sections.Select(s => s.Length).Max();

                string filePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Files", fileConfig.Name);
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, maxBuffer, FileOptions.SequentialScan))
                {
                    byte[] buffer = new byte[maxBuffer];
                    long bufferOffset = 0;

                    foreach(var section in fileConfig.Sections)
                    {
                        bufferOffset = section.StartOffset;
                        stream.Seek(bufferOffset, SeekOrigin.Begin);
                        int currentKeyOffset = -1;

                        // Search the first key in the section
                        byte[] currentKeyBytes = Encoding.UTF8.GetBytes(section.Keys.First());
                        while (currentKeyOffset == -1)
                        {
                            await stream.ReadAsync(buffer);
                            currentKeyOffset = SearchStringInBuffer(buffer, currentKeyBytes);
                        }

                        foreach (var language in Configuration.Languages)
                        {
                            for(int i = 0; i < section.Keys.Count; i++)
                            {
                                int nextIndex = i + 1;
                                if(nextIndex == section.Keys.Count)
                                {
                                    nextIndex = 0;
                                }

                                byte[] nextKeyBytes = Encoding.UTF8.GetBytes(section.Keys[nextIndex]);
                                (string value, int nextKeyOffset) = await FindValueAsync(buffer, currentKeyOffset, currentKeyBytes, nextKeyBytes);
                                values[language][section.Keys[i]] = value;

                                currentKeyOffset = nextKeyOffset;
                                currentKeyBytes = nextKeyBytes;
                            }

                            // Seek the start of the next first key
                            Index start = new Index(currentKeyOffset + currentKeyBytes.Length);
                            currentKeyOffset = currentKeyOffset + currentKeyBytes.Length + SearchStringInBuffer(buffer[start..], Encoding.UTF8.GetBytes(section.Keys.First()));
                        }
                    }
                }
            }

            // Write the values
            foreach(var language in values)
            {
                string filePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Output", $"{language.Key.ToLower()}.json");
                string fileContent = JsonSerializer.Serialize(language.Value, new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All),
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(filePath, fileContent);
            }

            Console.WriteLine($"Done.");
        }

        /// <summary>
        /// Returns the offset of the string in the buffer, or -1 if not found.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="stringToSearch"></param>
        /// <returns></returns>
        public static int SearchStringInBuffer(byte[] buffer, byte[] stringToSearch)
        {

            for (var i = 0; i <= (buffer.Length - stringToSearch.Length); i++)
            {
                if (buffer[i] == stringToSearch[0])
                {
                    for (int j = 1; j < stringToSearch.Length && buffer[i + j] == stringToSearch[j]; j++)
                    {
                        if (j == stringToSearch.Length - 1)
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }

        public static async Task<(string value, int nextKeyOffset)> FindValueAsync(byte[] buffer, int currentKeyOffset, byte[] currentKeyBytes, byte[] nextKeyBytes)
        {
            // Go to the end of the current key
            Index start = new Index(currentKeyOffset + currentKeyBytes.Length);
            buffer = buffer[start..];

            // Search for the start of the next key
            var nextKeyOffset = SearchStringInBuffer(buffer, nextKeyBytes);

            if(nextKeyOffset == -1)
            {
                throw new InvalidDataException("The next key is not in the buffer.");
            }

            // Read the string between the two keys
            byte[] between = buffer[0..nextKeyOffset];

            string result = Encoding.Unicode.GetString(between);
            // TODO
            // Strip everythin before '{S}' and everything after '\u0001'
            // Remove all '{*1}' and replace by strings

            return (result, currentKeyOffset + currentKeyBytes.Length + nextKeyOffset);
        }
    }
}
