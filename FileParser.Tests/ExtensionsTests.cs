using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace FileParser.Tests
{
    [TestClass]
    public class ExtensionsTests
    {
        private static IFileSystem GetFileSystem()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\foo\bar\file.json", new MockFileData(@"{ ""foo"": ""bar"" }") },
            }, @"c:\foo");

            return mockFileSystem;
        }

        [TestMethod]
        public void GetAbsolutePath_With_Absolute_BasePath_And_No_FileName_Should_Work()
        {
            var fileSystem = GetFileSystem();
            var expected = @"c:\foo\bar";
            var actual = fileSystem.GetAbsolutePath(@"c:\foo\bar");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetAbsolutePath_With_Absolute_BasePath_And_FileName_Should_Work()
        {
            var fileSystem = GetFileSystem();
            var expected = @"c:\foo\bar\file.json";
            var actual = fileSystem.GetAbsolutePath(@"c:\foo\bar", "file.json");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetAbsolutePath_With_Relative_BasePath_And_No_FileName_Should_Work()
        {
            var fileSystem = GetFileSystem();
            var expected = @"c:\foo\bar";
            var actual = fileSystem.GetAbsolutePath(@"./bar");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetAbsolutePath_With_Relative_BasePath_And_FileName_Should_Work()
        {
            var fileSystem = GetFileSystem();
            var expected = @"c:\foo\bar\file.json";
            var actual = fileSystem.GetAbsolutePath(@"./bar", "file.json");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetAbsolutePath_With_Mixed_PathSeparators_Should_Work()
        {
            var fileSystem = GetFileSystem();
            var expected = @"c:\foo\bar\file.json";
            var actual = fileSystem.GetAbsolutePath(@"c:\foo/bar", "file.json");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetAbsolutePath_With_Empty_BasePath_Should_Throw()
        {
            var fileSystem = GetFileSystem();
            fileSystem.GetAbsolutePath(string.Empty, "file.json");
        }
    }
}
