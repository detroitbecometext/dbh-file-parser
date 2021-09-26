using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FileParser.Config;

namespace FileParser.Extraction
{
    /// <summary>
    /// Handle the extraction of a single file.
    /// </summary>
    public class FileExtractor
    {
        private readonly FileConfig fileConfig;
        private readonly Dictionary<string, Dictionary<string, string>> values;
        private readonly SemaphoreSlim semaphore;
        private readonly IFileSystem fileSystem;

        public FileExtractor(FileConfig fileConfig, Dictionary<string, Dictionary<string, string>> values, SemaphoreSlim semaphore, IFileSystem fileSystem)
        {
            this.fileConfig = fileConfig;
            this.values = values;
            this.semaphore = semaphore;
            this.fileSystem = fileSystem;
        }

        private void OnProgressChanged(ProgressReport e)
        {
            EventHandler<ProgressReport> handler = ProgressChanged;
            handler.Invoke(this, e);
        }

        public event EventHandler<ProgressReport> ProgressChanged = delegate {};

        public async Task ExtractAsync(ExtractParameters parameters, CancellationToken token)
        {
            int maxBuffer = fileConfig.Sections.Max(s => s.Length);

            string filePath = fileSystem.GetAbsolutePath(parameters.Input, fileConfig.Name);

            if (!fileSystem.File.Exists(filePath))
            {
                throw new FileNotFoundException($"Cannot find the file '{filePath}'.", filePath);
            }

            using (var stream = fileSystem.FileStream.Create(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, maxBuffer, FileOptions.SequentialScan))
            {
                byte[] buffer = new byte[maxBuffer];
                long bufferOffset = 0;

                foreach ((SectionConfig section, int sectionIndex) in fileConfig.Sections
                    .OrderBy(s => s.StartOffset)
                    .Select((s, i) => (section: s, sectionIndex: i)))
                {
                    bufferOffset = section.StartOffset;
                    stream.Seek(bufferOffset, SeekOrigin.Begin);
                    int currentKeyOffset = -1;

                    // Search the first key in the section
                    byte[] currentKeyBytes = Encoding.UTF8.GetBytes(section.Keys[0]);
                    while (currentKeyOffset == -1)
                    {
                        await stream.ReadAsync(buffer, token).ConfigureAwait(false);
                        currentKeyOffset = Utils.FindOffsets(buffer, currentKeyBytes).First();
                    }

                    for (int languageIndex = 0; languageIndex < Configuration.Languages.Length; languageIndex++)
                    {
                        var language = Configuration.Languages[languageIndex];

                        for (int i = 0; i < section.Keys.Count; i++)
                        {
                            token.ThrowIfCancellationRequested();

                            int nextIndex = i + 1;
                            if (nextIndex == section.Keys.Count)
                            {
                                nextIndex = 0;
                            }

                            bool getUntilEndOfBuffer = false;
                            if (nextIndex == 0 && languageIndex == Configuration.Languages.Length - 1 && !section.HasKeyListing)
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
                            catch (InvalidDataException)
                            {
                                throw new InvalidDataException($"The key \"{section.Keys[nextIndex]}\" couldn't be found in the buffer.");
                            }

                            await semaphore.WaitAsync(token).ConfigureAwait(false);
                            values[language][section.Keys[i]] = value;
                            semaphore.Release();

                            currentKeyOffset = nextKeyOffset;
                            currentKeyBytes = nextKeyBytes;
                        }

                        if (section.HasKeyListing && languageIndex != Configuration.Languages.Length - 1)
                        {
                            // Skip over the key listing by searching the start of the next language
                            // i.e the next first key
                            // we also have to check if we have keys that start with the same string as the first key,
                            // to avoid stopping at a too early offset
                            int duplicates = section.Keys.Count(k => k != section.Keys[0] && k.StartsWith(section.Keys[0]));
                            var start = new Index(currentKeyOffset + currentKeyBytes.Length);

                            try
                            {
                                int offset = Utils.FindOffsets(buffer[start..], Encoding.UTF8.GetBytes(section.Keys[0])).ElementAt(duplicates);
                                currentKeyOffset = currentKeyOffset + currentKeyBytes.Length + offset;
                            }
                            catch
                            {
                                throw new InvalidDataException($"Couldn't find key \"{section.Keys[0]}\" with index {duplicates} in the listing.");
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

        /// <summary>
        /// Extract the value between two translation keys from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="currentKeyOffset">The offset of the current translation key.</param>
        /// <param name="currentKeyBytes">The bytes of the current translation key.</param>
        /// <param name="nextKeyBytes">The bytes of the next translation key.</param>
        /// <param name="getUntilEndOfBuffer">If true, extract the value from the current offset to the end of the buffer.</param>
        /// <returns>The extracted value and the next key's offset.</returns>
        private static (string value, int nextKeyOffset) FindValue(byte[] buffer, int currentKeyOffset, byte[] currentKeyBytes, byte[] nextKeyBytes, bool getUntilEndOfBuffer)
        {
            // Go to the end of the current key
            var start = new Index(currentKeyOffset + currentKeyBytes.Length);
            buffer = buffer[start..];

            byte[] between = buffer;
            var nextKeyOffset = 0;
            if (!getUntilEndOfBuffer)
            {
                // Search for the start of the next key
                nextKeyOffset = Utils.FindOffsets(buffer, nextKeyBytes).First();

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
