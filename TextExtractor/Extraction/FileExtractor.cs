using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextExtractor.Config;

namespace TextExtractor.Extraction
{
    public class FileExtractor
    {
        private readonly FileConfig fileConfig;
        private readonly Dictionary<string, Dictionary<string, string>> values;
        private readonly object lockObject;

        public FileExtractor(FileConfig fileConfig, Dictionary<string, Dictionary<string, string>> values, object lockObject)
        {
            this.fileConfig = fileConfig;
            this.values = values;
            this.lockObject = lockObject;
        }

        private void OnProgressChanged(ProgressReport e)
        {
            EventHandler<ProgressReport> handler = ProgressChanged;
            handler?.Invoke(this, e);
        }

        public event EventHandler<ProgressReport> ProgressChanged;

        public async Task ExtractAsync()
        {
            int maxBuffer = fileConfig.Sections.Select(s => s.Length).Max();

            string filePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Files", fileConfig.Name);
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, maxBuffer, FileOptions.SequentialScan))
            {
                byte[] buffer = new byte[maxBuffer];
                long bufferOffset = 0;

                foreach((SectionConfig section, int sectionIndex) in fileConfig.Sections.OrderBy(s => s.StartOffset).Select((s, i) => (section: s, sectionIndex: i)))
                {
                    bufferOffset = section.StartOffset;
                    stream.Seek(bufferOffset, SeekOrigin.Begin);
                    int currentKeyOffset = -1;

                    // Search the first key in the section
                    byte[] currentKeyBytes = Encoding.UTF8.GetBytes(section.Keys.First());
                    while (currentKeyOffset == -1)
                    {
                        await stream.ReadAsync(buffer);
                        currentKeyOffset = Utils.SearchStringInBuffer(buffer, currentKeyBytes);
                    }

                    for (int languageIndex = 0; languageIndex < Configuration.Languages.Count(); languageIndex++)
                    {
                        var language = Configuration.Languages.ElementAt(languageIndex);

                        for (int i = 0; i < section.Keys.Count; i++)
                        {
                            int nextIndex = i + 1;
                            if (nextIndex == section.Keys.Count)
                            {
                                nextIndex = 0;
                            }

                            bool getUntilEndOfBuffer = false;
                            if (nextIndex == 0 && languageIndex == Configuration.Languages.Count() - 1 && !section.HasKeyListing)
                            {
                                // We're at the last key of the last language and can't rely on the next first key to get the value,
                                // so we just get until the end of the buffer
                                getUntilEndOfBuffer = true;
                            }

                            byte[] nextKeyBytes = Encoding.UTF8.GetBytes(section.Keys[nextIndex]);
                            string value = string.Empty;
                            int nextKeyOffset = -1;
                            try
                            {
                                (value, nextKeyOffset) = FindValue(buffer, currentKeyOffset, currentKeyBytes, nextKeyBytes, getUntilEndOfBuffer);
                            }
                            catch (InvalidDataException e)
                            {
                                throw new InvalidDataException($"The key \"{section.Keys[nextIndex]}\" couldn't be found in the buffer.");
                            }

                            lock (lockObject)
                            {
                                values[language][section.Keys[i]] = value;
                            }

                            currentKeyOffset = nextKeyOffset;
                            currentKeyBytes = nextKeyBytes;
                        }

                        if (section.HasKeyListing && languageIndex != Configuration.Languages.Count() - 1)
                        {
                            // Skip over the key listing by searching the start of the next language
                            // i.e the next first key
                            // we also have to check if we have keys that start with the same string as the first key,
                            // to avoid stopping at a too early offset
                            int duplicates = section.Keys.Where(k => k.StartsWith(section.Keys.First())).Count();
                            for(int i = 0; i < duplicates; i++)
                            {
                                Index start = new Index(currentKeyOffset + currentKeyBytes.Length);
                                int searchOffset = Utils.SearchStringInBuffer(buffer[start..], Encoding.UTF8.GetBytes(section.Keys.First()));

                                if(searchOffset == -1)
                                {
                                    throw new InvalidDataException($"Couldn't find key \"{section.Keys.First()}\" with index {i} in the listing.");
                                }

                                currentKeyOffset = currentKeyOffset + currentKeyBytes.Length + searchOffset;
                            }
                        }

                        OnProgressChanged(new ProgressReport()
                        {
                            FileName = fileConfig.Name,
                            LanguageIndex = languageIndex,
                            SectionCount = fileConfig.Sections.Count,
                            SectionIndex = sectionIndex
                        });
                    }
                }
            }
        }

        private (string value, int nextKeyOffset) FindValue(byte[] buffer, int currentKeyOffset, byte[] currentKeyBytes, byte[] nextKeyBytes, bool getUntilEndOfBuffer)
        {
            // Go to the end of the current key
            Index start = new Index(currentKeyOffset + currentKeyBytes.Length);
            buffer = buffer[start..];

            byte[] between = buffer;
            var nextKeyOffset = 0;
            if (!getUntilEndOfBuffer)
            {
                // Search for the start of the next key
                nextKeyOffset = Utils.SearchStringInBuffer(buffer, nextKeyBytes);

                if (nextKeyOffset == -1)
                {
                    throw new InvalidDataException("The next key is not in the buffer.");
                }

                // Read the string between the two keys
                between = buffer[0..nextKeyOffset];
            }

            string result = Encoding.Unicode.GetString(between);

            // Strings will always have a random character at index 0, and the (char)0 character at index 1,
            // so we can remove those
            result = result[new Index(2)..];

            // Most strings end with \u0001, but player choices end with \u0002
            int endIndex = result.IndexOfAny(new char[] { (char)1, (char)2 });
            if (endIndex != -1)
            {
                result = result[..new Index(endIndex)];
            }

            // Dialogues strings start with {S}, other strings start at index 0
            int startIndex = result.IndexOf("{S}");
            if (startIndex != -1)
            {
                result = result[new Index(startIndex + 3)..];
            }

            // Remove QD formatting tags
            result = result.Replace("<QD_BR>", " ");
            result = result.Replace("<QD_THIN>", " ");
            result = result.Replace("<QD_NORMAL>", " ");

            // Remove double spaces
            result = Regex.Replace(result, @"\s{2,}", " ");

            // Trim the result
            result = result.Trim();

            return (result, currentKeyOffset + currentKeyBytes.Length + nextKeyOffset);
        }
    }
}
