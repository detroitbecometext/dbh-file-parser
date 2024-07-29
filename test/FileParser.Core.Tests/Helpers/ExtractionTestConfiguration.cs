using System.IO.Abstractions.TestingHelpers;

namespace FileParser.Core.Tests.Helpers;

internal class ExtractionTestConfiguration
{
    public required MockFileSystem FileSystem { get; init; }
    public required string ExpectedValues { get; init; }
}
