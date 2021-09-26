using FileParser.Config;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace FileParser.Tests.Helpers
{
    public static class TestHelpers
    {
        public static ExtractionTestConfiguration GetExtractionTestConfiguration(bool withKeyListing)
        {
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
                    Key = "X0001_FOO_FOO",
                    Value = "Foo Foo.",
                    Type = MockTranslationValueType.Dialogue
                },
                new()
                {
                    Key = "X0001_FOO_BAR",
                    Value = "Foo Bar.",
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

            var configuration = new Configuration()
            {
                Files = new List<FileConfig>()
                {
                    new()
                    {
                        Name = "file.dat",
                        Sections = new List<SectionConfig>()
                        {
                            new()
                            {
                                StartOffset = 0,
                                EndOffset = firstSection.Length,
                                HasKeyListing = withKeyListing,
                                Keys = firstSectionKeys.Select(s => s.Key).ToList()
                            },
                            new()
                            {
                                StartOffset = firstSection.Length + buffer.Length,
                                EndOffset = fileContent.Length,
                                HasKeyListing = withKeyListing,
                                Keys = secondSectionKeys.Select(s => s.Key).ToList()
                            }
                        }
                    }
                }
            };

            string serializedConfig = JsonSerializer.Serialize<Configuration>(configuration);

            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\foo\bar\config.json", new MockFileData(serializedConfig) },
                { @"c:\foo\input\file.dat", new MockFileData(fileContent) },
            }, @"c:\foo");

            Dictionary<string, string> values = firstSectionKeys.Concat(secondSectionKeys).ToDictionary(v => v.Key, v => v.Value);
            string expectedExtractedValues = JsonSerializer.Serialize(values, new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All)
            });

            return new ExtractionTestConfiguration()
            {
                Configuration = configuration,
                FileSystem = mockFileSystem,
                ExpectedValues = expectedExtractedValues
            };
        }

        private static byte[] GetSectionBytes(List<MockTranslationValue> values, bool withKeyListing)
        {
            List<byte> result = new();

            foreach(var language in Configuration.Languages)
            {
                result.AddRange(Encoding.UTF8.GetBytes(language));
                result.Add(0);
                foreach(var value in values)
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
}
