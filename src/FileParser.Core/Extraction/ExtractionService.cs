using FileParser.Core.Utils;
using System.IO.Abstractions;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;

namespace FileParser.Core.Extraction;

internal partial class ExtractionService : IExtractionService
{
    [GeneratedRegex("BigFile_PC\\.d\\d{2}$")]
    private static partial Regex BigFileRegex();

    private readonly IFileSystem fileSystem;

    public ExtractionService(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    /// <summary>
    /// Extract the localization data from the BigFile_PC.d* files.
    /// Logic translated from AreciboAntenna's unpacker (see scripts/unpacker.py).
    /// </summary>
    /// <param name="inputFolder">Path to the folder where the input files are.</param>
    /// <param name="outputFolder">Path to the folder where the output files will be saved.</param>
    /// <param name="verbose">Verbose if true.</param>
    /// <param name="callbacks">Callbacks to call during the extraction process.</param>
    public void Extract(string inputFolder, string outputFolder, ExtractionCallbacks callbacks)
    {
        List<byte[]> buffers = GetLocalizationBuffers(inputFolder);
        callbacks.OnBuffersDone(buffers.Count);

        Unpack(buffers, outputFolder, callbacks);
    }

    private List<byte[]> GetLocalizationBuffers(string inputFolder)
    {
        List<string> bigFiles = fileSystem.Directory
            .EnumerateFiles(inputFolder, "BigFile_PC.d*")
            .Where(f => BigFileRegex().IsMatch(f))
            .ToList();

        // Big files + BigFile_PC.dat
        BinaryReader[] fileStreams = new BinaryReader[bigFiles.Count + 1];
        fileStreams[0] = new BinaryReader(fileSystem.File.OpenRead(fileSystem.Path.Combine(inputFolder, "BigFile_PC.dat")));

        foreach ((string filePath, int index) in bigFiles.Select((f, i) => (f, i)))
        {
            fileStreams[index + 1] = new BinaryReader(fileSystem.File.OpenRead(filePath));
        }

        List<byte[]> localizationBuffers = [];

        // Get all buffers
        using (var indexFileReader = new BinaryReader(fileSystem.File.OpenRead(fileSystem.Path.Combine(inputFolder, "BigFile_PC.idx"))))
        {
            // offset copied from https://www.deadray.com/detroit/js/mod.v6.js
            indexFileReader.BaseStream.Seek(105, SeekOrigin.Begin);
            var entryLength = 28;

            while (true)
            {
                byte[] entryData = indexFileReader.ReadBytes(entryLength);

                // step copied from https://www.deadray.com/detroit/js/mod.v6.js
                if (entryData.Length != entryLength)
                {
                    break;
                }

                using var entryReader = new BinaryReader(new MemoryStream(entryData));

                int dataType = BinaryUtils.ReadInt32BigEndian(entryReader);
                int _ = BinaryUtils.ReadInt32BigEndian(entryReader); // This value always seems to be 1
                int dataId = BinaryUtils.ReadInt32BigEndian(entryReader);
                int offset = BinaryUtils.ReadInt32BigEndian(entryReader);
                int size = BinaryUtils.ReadInt32BigEndian(entryReader);
                int unknownByte = BinaryUtils.ReadInt32BigEndian(entryReader);
                int bigfileIdx = BinaryUtils.ReadInt32BigEndian(entryReader);

                if (dataType == 1016)
                {
                    fileStreams[bigfileIdx].BaseStream.Seek(offset, SeekOrigin.Begin);
                    byte[] buffer = fileStreams[bigfileIdx].ReadBytes(size);
                    localizationBuffers.Add(buffer);
                }
            }
        }

        // Close all streams
        foreach (var stream in fileStreams)
        {
            stream.Close();
        }

        return localizationBuffers;
    }

    private void Unpack(List<byte[]> buffers, string outputPath, ExtractionCallbacks callbacks)
    {
        // Mapping of languages to the <key, value> localization strings
        Dictionary<string, Dictionary<string, string>> values = new();

        foreach ((byte[] buffer, int index) in buffers.Select((b, i) => (b, i)))
        {
            try
            {
                ProcessBuffer(buffer, values, callbacks);
            }
            catch (Exception e)
            {
                callbacks.OnError($"Error processing buffer {index}: {e.Message}", e);
            }

            callbacks.OnBufferProcessed();
        }

        callbacks.OnFileWriteStarted(values.Count);

        if(!fileSystem.Directory.Exists(outputPath))
        {
            fileSystem.Directory.CreateDirectory(outputPath);
        }

        foreach (var language in values)
        {
            string filePath = fileSystem.Path.Combine(outputPath, $"{language.Key.ToLower()}.json");
            string fileContent = JsonSerializer.Serialize(language.Value, new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All)
            });
            fileSystem.File.WriteAllText(filePath, fileContent);
            callbacks.OnFileWritten();
        }
    }

    private static void ProcessBuffer(byte[] buffer, Dictionary<string, Dictionary<string, string>> values, ExtractionCallbacks callbacks)
    {
        byte[] localizBinary = Encoding.UTF8.GetBytes("LOCALIZ_");
        List<int> offsets = BinaryUtils.FindOffsets(buffer, localizBinary).ToList();

        if (offsets.Count != 1)
        {
            throw new InvalidOperationException("Cannot find LOCALIZ_");
        }

        int infoOffset = offsets.Single() + localizBinary.Length;

        int locaType = BitConverter.ToInt32(buffer.AsSpan()[infoOffset..(infoOffset + 4)]);
        int contentSize = BitConverter.ToInt32(buffer.AsSpan()[(infoOffset + 4)..(infoOffset + 8)]);
        int langCount = BitConverter.ToInt32(buffer.AsSpan()[(infoOffset + 8)..(infoOffset + 12)]);

        if (!(locaType == 6 || locaType == 5))
        {
            throw new InvalidOperationException("Invalid loca type");
        }

        if (contentSize != buffer.Length - infoOffset - 8)
        {
            throw new InvalidOperationException("Invalid content size");
        }

        // 05 type only provide English and French text, 06 is all
        byte[] contentBlock;
        if (locaType == 5)
        {
            // 05 type has an extra padding byte, so we need to skip it and read the lang count
            langCount = BitConverter.ToInt32(buffer.AsSpan()[(infoOffset + 9)..(infoOffset + 13)]);
            contentBlock = buffer[(infoOffset + 13)..];
        }
        else
        {
            contentBlock = buffer[(infoOffset + 12)..];
        }

        // every language starts with 01 03 00 00 4-byte identifier, followed by a 4-byte content, such as 00 46 52 45 (decoded to \x00FRE, meaning French)

        while (langCount != 0)
        {
            langCount--;
            ReadOnlySpan<byte> langSpecifier = contentBlock.AsSpan()[..4];

            if (!IsLangSpecifier(langSpecifier))
            {
                callbacks.OnWarning($"Invalid lang specifier {Encoding.ASCII.GetString(langSpecifier)}");
                break;
            }

            // Skip contentBlock[4], which is 0x00
            string currentLang = Encoding.ASCII.GetString(contentBlock[5..8]);
            contentBlock = contentBlock[8..];

            while (true)
            {
                if (!contentBlock.Any())
                {
                    break;
                }

                int unknownKeyHeader = BitConverter.ToInt32(contentBlock.AsSpan()[..4]);
                int currentKeyLength = BitConverter.ToInt32(contentBlock.AsSpan()[4..8]);
                contentBlock = contentBlock[8..];

                if (currentKeyLength == 0)
                {
                    // End of current language
                    break;
                }

                // Potentially there's a listing of all keys of the current language that we need to skip
                // In this case the next 4 bytes will be the number of keys in the listing

                if (256 > BitConverter.ToInt32(contentBlock.AsSpan()[..4]))
                {
                    // Here currentKeyLength is the number of keys in the listing

                    for (int listingKeyIndex = 0; listingKeyIndex < currentKeyLength; listingKeyIndex++)
                    {
                        int listingKeyLength = BitConverter.ToInt32(contentBlock.AsSpan()[..4]);
                        contentBlock = contentBlock[4..];

                        // Skip the key
                        contentBlock = contentBlock[listingKeyLength..];

                        // Skip the content
                        if (contentBlock[0] == 1)
                        {
                            contentBlock = contentBlock[10..];
                        }
                        else if (contentBlock[0] == 0)
                        {
                            contentBlock = contentBlock[1..];
                        }
                        else
                        {
                            throw new InvalidOperationException("Invalid content block");
                        }
                    }

                    // Check if we need to switch to the next language
                    if (contentBlock.Length != 0 && IsLangSpecifier(contentBlock.AsSpan()[..4]))
                    {
                        break;
                    }

                    // This is the last language for this block
                    continue;
                }

                string key = ReadKey(currentKeyLength, contentBlock);
                contentBlock = contentBlock[currentKeyLength..];

                int textLength = BitConverter.ToInt32(contentBlock.AsSpan()[..4]);
                byte[] text = contentBlock[4..(4 + textLength)];

                if (!values.ContainsKey(currentLang))
                {
                    values.Add(currentLang, new Dictionary<string, string>());
                }

                if (values[currentLang].ContainsKey(key))
                {
                    string oldValue = values[currentLang][key];
                    string newValue = FormatValue(text);
                    if (oldValue != newValue && !string.IsNullOrWhiteSpace(newValue))
                    {
                        callbacks.OnWarning($"Duplicate key {key} in {currentLang}, old value: {oldValue}, new value: {newValue}");
                    }
                }
                else
                {
                    values[currentLang].Add(key, FormatValue(text));
                }

                contentBlock = contentBlock[(4 + textLength)..];

                if (IsLangSpecifier(contentBlock.AsSpan()[..4]))
                {
                    // Next language
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Check if an array of bytes is a language specifier.
    /// </summary>
    /// <param name="langSpecifier">The array of bytes.</param>
    /// <returns>True if the data match the lang specifier.</returns>
    private static bool IsLangSpecifier(ReadOnlySpan<byte> langSpecifier)
    {
        return langSpecifier is [01, 03, 00, 00];
    }

    /// <summary>
    /// Read a key from a content block.
    /// </summary>
    /// <param name="keyLength">The length of the key.</param>
    /// <param name="contentBlock">The content block.</param>
    /// <returns>The parsed key.</returns>
    private static string ReadKey(int keyLength, byte[] contentBlock)
    {
        return Encoding.ASCII.GetString(contentBlock[..keyLength]);
    }

    /// <summary>
    /// Format a string value to remove formatting tags.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <returns>The formatted string.</returns>
    private static string FormatValue(byte[] text)
    {
        string value = Encoding.Unicode.GetString(text);

        // Dialogues strings start with {S}, other strings start at index 0
        int startIndex = value.IndexOf("{S}");
        if (startIndex != -1)
        {
            value = value[new Index(startIndex + 3)..];
        }

        // Remove QD formatting tags
        value = value.Replace("<QD_BR>", " ");
        value = value.Replace("<QD_THIN>", " ");
        value = value.Replace("<QD_NORMAL>", " ");

        // Remove double spaces
        value = Regex.Replace(value, @"\s{2,}", " ");

        // Trim the result
        value = value.Trim();

        return value;
    }
}
