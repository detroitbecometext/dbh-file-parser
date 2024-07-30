using FileParser.Core.Extraction;
using FileParser.Core.Tests.Helpers;
using System.IO.Abstractions.TestingHelpers;

namespace FileParser.Core.Tests.Extraction;

/// <summary>
/// Tests for the <see cref="ExtractionService"/> class.
/// </summary>
[TestFixture]
internal class ExtractionServiceTests
{
    [Test]
    public void Extract_With_Invalid_Source_Folder_Should_Throw()
    {
        var mockFileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData>(),
            @"c:\foo");

        IExtractionService service = new ExtractionService(mockFileSystem);
        Assert.That(() => service.Extract(@"c:\foo\bar\input", "./output", ExtractionCallbacks.Empty), Throws.InstanceOf<DirectoryNotFoundException>());
    }

    [Test]
    public void Extract_With_No_Source_Files_Should_Throw()
    {
        var mockFileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData>()
            {
                { @"c:\foo\bar\input", new MockDirectoryData() }
            },
            @"c:\foo");

        IExtractionService service = new ExtractionService(mockFileSystem);
        Assert.That(() => service.Extract(@"c:\foo\bar\input", "./output", ExtractionCallbacks.Empty), Throws.InstanceOf<FileNotFoundException>());
    }

    [Test]
    [Ignore("Unignore when TestHelpers.GetExtractionTestConfiguration is updated")]
    public void Extract_With_Key_Listing_Should_Work_Async()
    {
        ExtractionTestConfiguration testConfig = TestHelpers.GetExtractionTestConfiguration(true);

        IExtractionService service = new ExtractionService(testConfig.FileSystem);

        service.Extract(@"c:\foo\input", "./output", ExtractionCallbacks.Empty);

        Assert.IsTrue(testConfig.FileSystem.Directory.Exists(@"c:\foo\output"));
        Assert.That(testConfig.FileSystem.Directory.EnumerateFiles(@"c:\foo\output").Count(), Is.EqualTo(TestHelpers.Languages.Length));

        var actualResult = testConfig.FileSystem.File.ReadAllText(@"c:\foo\output\eng.json");
        Assert.That(actualResult, Is.EqualTo(testConfig.ExpectedValues));
    }

    [Test]
    [Ignore("Unignore when TestHelpers.GetExtractionTestConfiguration is updated")]
    public void Extract_Without_Key_Listing_Should_Work_Async()
    {
        ExtractionTestConfiguration testConfig = TestHelpers.GetExtractionTestConfiguration(false);

        IExtractionService service = new ExtractionService(testConfig.FileSystem);
        service.Extract(@"c:\foo\input", "./output", ExtractionCallbacks.Empty);

        Assert.IsTrue(testConfig.FileSystem.Directory.Exists(@"c:\foo\output"));
        Assert.That(testConfig.FileSystem.Directory.EnumerateFiles(@"c:\foo\output").Count(), Is.EqualTo(TestHelpers.Languages.Length));

        var actualResult = testConfig.FileSystem.File.ReadAllText(@"c:\foo\output\eng.json");
        Assert.That(actualResult, Is.EqualTo(testConfig.ExpectedValues));
    }
}
