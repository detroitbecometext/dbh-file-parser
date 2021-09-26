using FileParser.Config;
using FileParser.Core;
using FileParser.Extraction;
using FileParser.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileParser.Tests
{
    [TestClass]
    public class ExtractionServiceTests
    {
        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task Extract_With_No_Config_File_Should_Throw()
        {
            var reporter = Mock.Of<IExtractionProgressReporter>();
            var consoleWriter = Mock.Of<IConsoleWriter>();
            var argumentsProvider = Mock.Of<ICommandLineArgumentsProvider>(c => c.ExecutablePath == @"c:\foo\bar");
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\foo\input\file.dat", new MockFileData(string.Empty) },
            }, @"c:\foo");

            IExtractionService service = new ExtractionService(reporter, mockFileSystem, argumentsProvider, consoleWriter);
            await service.ExtractAsync(new ExtractParameters()
            {
                Input = "./input",
                Output = "./output",
                Verbose = false
            }, default);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task Extract_With_No_Source_Files_Should_Throw()
        {
            var reporter = Mock.Of<IExtractionProgressReporter>();
            var consoleWriter = Mock.Of<IConsoleWriter>();
            var argumentsProvider = Mock.Of<ICommandLineArgumentsProvider>(c => c.ExecutablePath == @"c:\foo\bar");
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\foo\bar\config.json", new MockFileData(JsonSerializer.Serialize<Configuration>(TestHelpers.GetExtractionTestConfiguration(false).Configuration)) },
            }, @"c:\foo");

            IExtractionService service = new ExtractionService(reporter, mockFileSystem, argumentsProvider, consoleWriter);
            await service.ExtractAsync(new ExtractParameters()
            {
                Input = "./input",
                Output = "./output",
                Verbose = false
            }, default);
        }

        [TestMethod]
        public async Task Extract_With_Key_Listing_Should_Work_Async()
        {
            var reporter = Mock.Of<IExtractionProgressReporter>();
            var consoleWriter = Mock.Of<IConsoleWriter>();
            var argumentsProvider = Mock.Of<ICommandLineArgumentsProvider>(c => c.ExecutablePath == @"c:\foo\bar");

            ExtractionTestConfiguration testConfig = TestHelpers.GetExtractionTestConfiguration(true);
            IExtractionService service = new ExtractionService(reporter, testConfig.FileSystem, argumentsProvider, consoleWriter);

            await service.ExtractAsync(new ExtractParameters()
            {
                Input = "./input",
                Output = "./output",
                Verbose = false
            }, default);

            Assert.IsTrue(testConfig.FileSystem.Directory.Exists(@"c:\foo\output"));
            Assert.AreEqual(Configuration.Languages.Length, testConfig.FileSystem.Directory.EnumerateFiles(@"c:\foo\output").Count());

            var actualResult = testConfig.FileSystem.File.ReadAllText(@"c:\foo\output\eng.json");
            Assert.AreEqual(testConfig.ExpectedValues, actualResult);
        }

        [TestMethod]
        public async Task Extract_Without_Key_Listing_Should_Work_Async()
        {
            var reporter = Mock.Of<IExtractionProgressReporter>();
            var consoleWriter = Mock.Of<IConsoleWriter>();
            var argumentsProvider = Mock.Of<ICommandLineArgumentsProvider>(c => c.ExecutablePath == @"c:\foo\bar");

            ExtractionTestConfiguration testConfig = TestHelpers.GetExtractionTestConfiguration(false);
            IExtractionService service = new ExtractionService(reporter, testConfig.FileSystem, argumentsProvider, consoleWriter);

            await service.ExtractAsync(new ExtractParameters()
            {
                Input = "./input",
                Output = "./output",
                Verbose = false
            }, default);

            Assert.IsTrue(testConfig.FileSystem.Directory.Exists(@"c:\foo\output"));
            Assert.AreEqual(Configuration.Languages.Length, testConfig.FileSystem.Directory.EnumerateFiles(@"c:\foo\output").Count());

            var actualResult = testConfig.FileSystem.File.ReadAllText(@"c:\foo\output\eng.json");
            Assert.AreEqual(testConfig.ExpectedValues, actualResult);
        }
    }
}
