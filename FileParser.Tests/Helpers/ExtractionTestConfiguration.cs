using FileParser.Config;
using System.IO.Abstractions.TestingHelpers;

namespace FileParser.Tests.Helpers
{
    public class ExtractionTestConfiguration
    {
        public Configuration Configuration { get; init; }
        public MockFileSystem FileSystem { get; init; }
        public string ExpectedValues { get; init; }
    }
}
