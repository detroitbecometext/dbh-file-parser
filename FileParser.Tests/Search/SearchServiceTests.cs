using FileParser.Config;
using FileParser.Core;
using FileParser.Search;
using FileParser.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileParser.Tests
{
    [TestClass]
    public class SearchServiceTests
    {
        [TestMethod]
        public async Task Search_With_Value_In_Buffer_Should_Work()
        {
            var output = new List<string>();
            var reporter = Mock.Of<ISearchProgressReporter>();
            var consoleWriter = Mock.Of<IConsoleWriter>();
            Mock.Get(consoleWriter)
                .Setup(c => c.WriteLine(It.IsAny<string>()))
                .Callback((string text) => output.Add(text));

            ExtractionTestConfiguration testConfig = TestHelpers.GetExtractionTestConfiguration(true);
            ISearchService service = new SearchService(testConfig.FileSystem, reporter, consoleWriter);

            await service.SearchAsync(new SearchParameters()
            {
                Input = "./input",
                Value = "Foo.",
                BufferSize = 8000,
                InKeys = false,
                Verbose = false
            }, default);

            Assert.AreEqual(1, output.Count);
            Assert.IsTrue(output.Single().StartsWith("Found in c:\\foo\\input\\file.dat at offsets"));

            Regex offsetsRegex = new Regex("\\d+");
            var offsets = offsetsRegex.Matches(output.Single());

            Assert.AreEqual(Configuration.Languages.Length, offsets.Count);
        }

        [TestMethod]
        public async Task Search_With_Value_Between_Two_Buffers_Should_Work()
        {
            var output = new List<string>();
            var reporter = Mock.Of<ISearchProgressReporter>();
            var consoleWriter = Mock.Of<IConsoleWriter>();
            Mock.Get(consoleWriter)
                .Setup(c => c.WriteLine(It.IsAny<string>()))
                .Callback((string text) => output.Add(text));

            ExtractionTestConfiguration testConfig = TestHelpers.GetExtractionTestConfiguration(true);
            ISearchService service = new SearchService(testConfig.FileSystem, reporter, consoleWriter);

            await service.SearchAsync(new SearchParameters()
            {
                Input = "./input",
                Value = "Foo Bar Baz.",
                BufferSize = Encoding.Unicode.GetByteCount("Foo Bar Baz.") + 4,
                InKeys = false,
                Verbose = false
            }, default);

            Assert.AreEqual(1, output.Count);
            Assert.IsTrue(output.Single().StartsWith("Found in c:\\foo\\input\\file.dat at offsets"));

            Regex offsetsRegex = new Regex("\\d+");
            var offsets = offsetsRegex.Matches(output.Single());

            Assert.AreEqual(Configuration.Languages.Length, offsets.Count);
        }

        [TestMethod]
        public async Task Search_With_Value_Not_In_Buffers_Should_Work()
        {
            var output = new List<string>();
            var reporter = Mock.Of<ISearchProgressReporter>();
            var consoleWriter = Mock.Of<IConsoleWriter>();
            Mock.Get(consoleWriter)
                .Setup(c => c.WriteLine(It.IsAny<string>()))
                .Callback((string text) => output.Add(text));

            ExtractionTestConfiguration testConfig = TestHelpers.GetExtractionTestConfiguration(true);
            ISearchService service = new SearchService(testConfig.FileSystem, reporter, consoleWriter);

            await service.SearchAsync(new SearchParameters()
            {
                Input = "./input",
                Value = "Test.",
                BufferSize = 8000,
                InKeys = false,
                Verbose = false
            }, default);

            Assert.AreEqual(1, output.Count);
            Assert.IsTrue(output.Single() == "Value not found.");
        }

        [TestMethod]
        public async Task Search_Wrong_Case_Should_Work()
        {
            var output = new List<string>();
            var reporter = Mock.Of<ISearchProgressReporter>();
            var consoleWriter = Mock.Of<IConsoleWriter>();
            Mock.Get(consoleWriter)
                .Setup(c => c.WriteLine(It.IsAny<string>()))
                .Callback((string text) => output.Add(text));

            ExtractionTestConfiguration testConfig = TestHelpers.GetExtractionTestConfiguration(true);
            ISearchService service = new SearchService(testConfig.FileSystem, reporter, consoleWriter);

            await service.SearchAsync(new SearchParameters()
            {
                Input = "./input",
                Value = "FOO.",
                BufferSize = 8000,
                InKeys = false,
                Verbose = false
            }, default);

            Assert.AreEqual(1, output.Count);
            Assert.IsTrue(output.Single() == "Value not found.");
        }

        [TestMethod]
        public async Task Search_In_Keys_Should_Work()
        {
            var output = new List<string>();
            var reporter = Mock.Of<ISearchProgressReporter>();
            var consoleWriter = Mock.Of<IConsoleWriter>();
            Mock.Get(consoleWriter)
                .Setup(c => c.WriteLine(It.IsAny<string>()))
                .Callback((string text) => output.Add(text));

            ExtractionTestConfiguration testConfig = TestHelpers.GetExtractionTestConfiguration(false);
            ISearchService service = new SearchService(testConfig.FileSystem, reporter, consoleWriter);

            await service.SearchAsync(new SearchParameters()
            {
                Input = "./input",
                Value = "X0001_FOO",
                BufferSize = 8000,
                InKeys = true,
                Verbose = false
            }, default);

            Assert.AreEqual(1, output.Count);
            Assert.IsTrue(output.Single().StartsWith("Found in c:\\foo\\input\\file.dat at offsets"));

            Regex offsetsRegex = new Regex("\\d+");
            var offsets = offsetsRegex.Matches(output.Single());

            Assert.AreEqual(Configuration.Languages.Length * 4, offsets.Count);
        }
    }
}
