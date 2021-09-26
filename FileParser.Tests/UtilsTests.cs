using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

namespace FileParser.Tests
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void SearchStringInBuffer_With_Match_Should_Return_Correct_Value()
        {
            var buffer = Encoding.UTF8.GetBytes("This is a string.");
            var searchString = Encoding.UTF8.GetBytes("string");
            var result = Utils.FindOffsets(buffer, searchString).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(10, result.Single());
        }

        [TestMethod]
        public void SearchStringInBuffer_With_No_Match_Should_Return_Correct_Value()
        {
            var buffer = Encoding.UTF8.GetBytes("This is a string.");
            var searchString = Encoding.UTF8.GetBytes("stringo");
            var result = Utils.FindOffsets(buffer, searchString).ToList();

            Assert.IsFalse(result.Any());
        }
    }
}
