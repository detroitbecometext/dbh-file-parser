using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var result = Utils.SearchStringInBuffer(buffer, searchString);
            Assert.AreEqual(10, result);
        }

        [TestMethod]
        public void SearchStringInBuffer_With_No_Match_Should_Return_Correct_Value()
        {
            var buffer = Encoding.UTF8.GetBytes("This is a string.");
            var searchString = Encoding.UTF8.GetBytes("stringo");
            var result = Utils.SearchStringInBuffer(buffer, searchString);
            Assert.AreEqual(-1, result);
        }
    }
}
