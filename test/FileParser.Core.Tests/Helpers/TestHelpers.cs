using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace FileParser.Core.Tests.Helpers;

internal static class TestHelpers
{
    public static readonly string[] Languages = [
        "ARA",
        "BRA",
        "CHI",
        "CRO",
        "CZE",
        "DAN",
        "DUT",
        "ENG",
        "FIN",
        "FRE",
        "GER",
        "GRE",
        "HUN",
        "ITA",
        "JAP", // Duplicate of JPN, has a few keys with empty values
        "JPN",
        "KOR",
        "MEX",
        "NOR",
        "POL",
        "POR",
        "RUS",
        "SCH",
        "SPA",
        "SWE",
        "TUR"
    ];

    /// <summary>
    /// Create a configuration file and the corresponding mock file system.
    /// </summary>
    /// <param name="withKeyListing">If true, will add key listing to the sections.</param>
    /// <param name="addUnknownKey">If true, will generate an invalid configuration by adding a key that is not in the file.</param>
    /// <returns>The configuration for an extraction test.</returns>
    public static ExtractionTestConfiguration GetExtractionTestConfiguration(bool withKeyListing)
    {
        // TODO: Mock BigFile.dat to fix tests

        List<MockTranslationValue> firstSectionKeys = new()
        {
            new()
            {
                Key = "X0001_FOO",
                Value = "Foo.",
                Type = MockTranslationValueType.Dialogue
            },
            new()
            {
                Key = "X0001_BAR",
                Value = "Bar.",
                Type = MockTranslationValueType.Dialogue
            },
            new()
            {
                Key = "X0001_BAZ",
                Value = "BAZ",
                Type = MockTranslationValueType.Choice
            }
        };

        List<MockTranslationValue> secondSectionKeys = new()
        {
            new()
            {
                Key = "X0001_FOO_BAR",
                Value = "Foo Bar.",
                Type = MockTranslationValueType.Dialogue
            },
            new()
            {
                Key = "X0001_FOO_BAR_BAZ",
                Value = "Foo Bar Baz.",
                Type = MockTranslationValueType.Dialogue
            },
            new()
            {
                Key = "X0001_FOO_BAZ",
                Value = "Baz.",
                Type = MockTranslationValueType.Dialogue
            }
        };

        var firstSection = GetSectionBytes(firstSectionKeys, withKeyListing);
        var buffer = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
        var secondSection = GetSectionBytes(secondSectionKeys, withKeyListing);

        var fileContent = firstSection.Concat(buffer).Concat(secondSection).ToArray();

        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\foo\input\file.dat", new MockFileData(fileContent) },
        }, @"c:\foo");

        Dictionary<string, string> values = firstSectionKeys.Concat(secondSectionKeys).ToDictionary(v => v.Key, v => v.Value);
        string expectedExtractedValues = JsonSerializer.Serialize(values, new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All)
        });

        return new ExtractionTestConfiguration()
        {
            FileSystem = mockFileSystem,
            ExpectedValues = expectedExtractedValues
        };
    }

    private static byte[] GetSectionBytes(List<MockTranslationValue> values, bool withKeyListing)
    {
        List<byte> result = new();

        foreach (var language in Languages)
        {
            result.AddRange(Encoding.UTF8.GetBytes(language));
            result.Add(0);
            foreach (var value in values)
            {
                result.AddRange(Encoding.UTF8.GetBytes(value.Key));
                result.AddRange(Encoding.Unicode.GetBytes(value.FormattedValue));
            }

            if (withKeyListing)
            {
                foreach (var key in values.Select(v => v.Key))
                {
                    result.AddRange(Encoding.UTF8.GetBytes(key));
                    result.Add(0);
                }
            }
        }

        return result.ToArray();
    }
}
