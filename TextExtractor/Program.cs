using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
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
                // TODO: temp
                if(fileConfig.Name != "BigFile_PC.d12")
                {
                    continue;
                }

                Console.WriteLine($"Exporting file '{fileConfig.Name}'...");

                int maxBuffer = fileConfig.Sections.Select(s => s.Length).Max();

                string filePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Files", fileConfig.Name);
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, maxBuffer, FileOptions.SequentialScan))
                {
                    byte[] buffer = new byte[maxBuffer];
                    long bufferOffset = 0;

                    for(int sectionIndex = 0; sectionIndex < fileConfig.Sections.Count; sectionIndex++)
                    {
                        Console.WriteLine($"Processing section {sectionIndex + 1} of {fileConfig.Sections.Count}...");
                        var section = fileConfig.Sections[sectionIndex];
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

                        for(int languageIndex = 0; languageIndex < Configuration.Languages.Count(); languageIndex++)
                        {
                            var language = Configuration.Languages.ElementAt(languageIndex);
                            Console.WriteLine($"Getting values for {language}...");

                            for (int i = 0; i < section.Keys.Count; i++)
                            {
                                int nextIndex = i + 1;
                                if(nextIndex == section.Keys.Count)
                                {
                                    nextIndex = 0;
                                }

                                bool getUntilEndOfBuffer = false;
                                if(languageIndex == Configuration.Languages.Count() - 1 && !section.HasKeyListing)
                                {
                                    // We're at the last language and can't rely on the next first key to get the value,
                                    // so we just get until the end of the buffer
                                    getUntilEndOfBuffer = true;
                                }

                                byte[] nextKeyBytes = Encoding.UTF8.GetBytes(section.Keys[nextIndex]);
                                (string value, int nextKeyOffset) = await FindValueAsync(buffer, currentKeyOffset, currentKeyBytes, nextKeyBytes, getUntilEndOfBuffer);
                                values[language][section.Keys[i]] = value;

                                currentKeyOffset = nextKeyOffset;
                                currentKeyBytes = nextKeyBytes;
                            }

                            if (section.HasKeyListing)
                            {
                                // Skip over the key listing by searching the start of the next language
                                // i.e the next first key
                                Index start = new Index(currentKeyOffset + currentKeyBytes.Length);
                                currentKeyOffset = currentKeyOffset + currentKeyBytes.Length + SearchStringInBuffer(buffer[start..], Encoding.UTF8.GetBytes(section.Keys.First()));
                            }
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

        public static async Task<(string value, int nextKeyOffset)> FindValueAsync(byte[] buffer, int currentKeyOffset, byte[] currentKeyBytes, byte[] nextKeyBytes, bool getUntilEndOfBuffer)
        {
            // Go to the end of the current key
            Index start = new Index(currentKeyOffset + currentKeyBytes.Length);
            buffer = buffer[start..];

            byte[] between = buffer;
            var nextKeyOffset = 0;
            if (!getUntilEndOfBuffer)
            {
                // Search for the start of the next key
                nextKeyOffset = SearchStringInBuffer(buffer, nextKeyBytes);

                if (nextKeyOffset == -1)
                {
                    throw new InvalidDataException("The next key is not in the buffer.");
                }

                // Read the string between the two keys
                between = buffer[0..nextKeyOffset];
            }

            string result = Encoding.Unicode.GetString(between);

            // Strip everythin before '{S}' and everything after '\u0001'
            int startIndex = result.IndexOf("{S}");
            if (startIndex != -1)
            {
                result = result[new Index(startIndex + 3)..];
            }
            int endIndex = result.IndexOf("\u0001");
            if (endIndex != -1)
            {
                result = result[..new Index(endIndex)];
            }

            // Some strings also start with "\0" instead of "{S}"
            startIndex = result.IndexOf((char)0);
            if (startIndex != -1)
            {
                result = result[new Index(startIndex + 1)..];
            }

            // Remove QD formatting tags
            result = result.Replace("<QD_BR>", " ");
            result = result.Replace("<QD_THIN>", " ");
            result = result.Replace("<QD_NORMAL>", " ");

            // Swap problematic unicode characters
            // No-Break Space (160) to space (32)
            result = result.Replace((char)160, (char)32);

            // Trim the result
            result = result.Trim();

            // Leave apostrophes (') as unicode in the exported file (\u0027)

            return (result, currentKeyOffset + currentKeyBytes.Length + nextKeyOffset);
        }
    }
}
