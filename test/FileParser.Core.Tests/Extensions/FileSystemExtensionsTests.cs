using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FileParser.Core.Extensions;

namespace FileParser.Core.Tests.Extensions;

[TestFixture]
internal class FileSystemExtensionsTests
{
    private static IFileSystem GetFileSystem()
    {
        var mockFileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData>
            {
                {
                    @"c:\foo\bar\file.json", new MockFileData(@"{ ""foo"": ""bar"" }")
                },
            },
            @"c:\foo");

        return mockFileSystem;
    }

    [Test]
    public void GetAbsolutePath_With_Absolute_BasePath_And_No_FileName_Should_Work()
    {
        var fileSystem = GetFileSystem();
        var expected = @"c:\foo\bar";
        var actual = fileSystem.GetAbsolutePath(@"c:\foo\bar");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetAbsolutePath_With_Absolute_BasePath_And_FileName_Should_Work()
    {
        var fileSystem = GetFileSystem();
        var expected = @"c:\foo\bar\file.json";
        var actual = fileSystem.GetAbsolutePath(@"c:\foo\bar", "file.json");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetAbsolutePath_With_Relative_BasePath_And_No_FileName_Should_Work()
    {
        var fileSystem = GetFileSystem();
        var expected = @"c:\foo\bar";
        var actual = fileSystem.GetAbsolutePath(@"./bar");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetAbsolutePath_With_Relative_BasePath_And_FileName_Should_Work()
    {
        var fileSystem = GetFileSystem();
        var expected = @"c:\foo\bar\file.json";
        var actual = fileSystem.GetAbsolutePath(@"./bar", "file.json");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetAbsolutePath_With_Mixed_PathSeparators_Should_Work()
    {
        var fileSystem = GetFileSystem();
        var expected = @"c:\foo\bar\file.json";
        var actual = fileSystem.GetAbsolutePath(@"c:\foo/bar", "file.json");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetAbsolutePath_With_Empty_BasePath_Should_Throw()
    {
        var fileSystem = GetFileSystem();
        Assert.That(() => fileSystem.GetAbsolutePath(string.Empty, "file.json"), Throws.ArgumentException);
    }
}
