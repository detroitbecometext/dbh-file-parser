﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextExtractor.Config;

namespace TextExtractor.Extraction
{
    public class FileExtractor
    {
        private void OnProgressChanged(ProgressReport e)
        {
            EventHandler<ProgressReport> handler = ProgressChanged;
            handler?.Invoke(this, e);
        }

        public event EventHandler<ProgressReport> ProgressChanged;

        public async Task ExtractAsync(FileConfig fileConfig, ConcurrentDictionary<string, ConcurrentDictionary<string, string>> values)
        {
            int maxBuffer = fileConfig.Sections.Select(s => s.Length).Max();

            string filePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Files", fileConfig.Name);
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, maxBuffer, FileOptions.SequentialScan))
            {
                byte[] buffer = new byte[maxBuffer];
                long bufferOffset = 0;

                for (int sectionIndex = 0; sectionIndex < fileConfig.Sections.Count; sectionIndex++)
                {
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
                            if (languageIndex == Configuration.Languages.Count() - 1 && !section.HasKeyListing)
                            {
                                // We're at the last language and can't rely on the next first key to get the value,
                                // so we just get until the end of the buffer
                                getUntilEndOfBuffer = true;
                            }

                            byte[] nextKeyBytes = Encoding.UTF8.GetBytes(section.Keys[nextIndex]);
                            (string value, int nextKeyOffset) = FindValue(buffer, currentKeyOffset, currentKeyBytes, nextKeyBytes, getUntilEndOfBuffer);
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

        /// <summary>
        /// Returns the offset of the string in the buffer, or -1 if not found.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="stringToSearch"></param>
        /// <returns></returns>
        private int SearchStringInBuffer(byte[] buffer, byte[] stringToSearch)
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

            // Remove double spaces
            result = result.Replace("  ", " ");

            // Trim the result
            result = result.Trim();

            // Leave apostrophes (') as unicode in the exported file (\u0027)

            return (result, currentKeyOffset + currentKeyBytes.Length + nextKeyOffset);
        }
    }
}