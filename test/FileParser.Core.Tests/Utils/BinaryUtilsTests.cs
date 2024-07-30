using FileParser.Core.Utils;
using System.Text;

namespace FileParser.Core.Tests.Utils;

[TestFixture]
public class BinaryUtilsTests
{
    [Test]
    public void FindOffsets_With_Match_Should_Return_Correct_Value()
    {
        var buffer = Encoding.UTF8.GetBytes("This is a string.");
        var searchString = Encoding.UTF8.GetBytes("string");
        var result = BinaryUtils.FindOffsets(buffer, searchString).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.Single(), Is.EqualTo(10));
    }

    [Test]
    public void FindOffsets_With_Unicode_Match_Should_Return_Correct_Value()
    {
        var buffer = Encoding.Unicode.GetBytes("This is a string.");
        var searchString = Encoding.Unicode.GetBytes("string");
        var result = BinaryUtils.FindOffsets(buffer, searchString).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.Single(), Is.EqualTo(20));
    }

    [Test]
    public void FindOffsets_With_Matches_Should_Return_Correct_Values()
    {
        var buffer = Encoding.UTF8.GetBytes("This is a string, and another string.");
        var searchString = Encoding.UTF8.GetBytes("string");
        var result = BinaryUtils.FindOffsets(buffer, searchString).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(10));
        Assert.That(result[^1], Is.EqualTo(30));
    }

    [Test]
    public void FindOffsets_With_No_Match_Should_Return_Correct_Value()
    {
        var buffer = Encoding.UTF8.GetBytes("This is a string.");
        var searchString = Encoding.UTF8.GetBytes("stringo");
        var result = BinaryUtils.FindOffsets(buffer, searchString).ToList();

        Assert.That(result, Has.Count.EqualTo(0));
    }
}
