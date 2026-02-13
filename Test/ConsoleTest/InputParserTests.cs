using EasySave.Utilities;

namespace ConsoleTest;

public class InputParserTests
{
    private readonly InputParser _parser = new();

    [Fact]
    public void ParseJobRange_ValidRange_ShouldReturnSequentialIds()
    {
        var result = _parser.ParseJobRange("1-3");
        Assert.Equal(new List<int> { 1, 2, 3 }, result);
    }

    [Fact]
    public void ParseJobRange_SingleValue_ShouldReturnSingleElement()
    {
        var result = _parser.ParseJobRange("2-2");
        Assert.Equal(new List<int> { 2 }, result);
    }

    [Fact]
    public void ParseJobRange_WithWhitespace_ShouldTrimAndParse()
    {
        var result = _parser.ParseJobRange(" 1 - 3 ");
        Assert.Equal(new List<int> { 1, 2, 3 }, result);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1-")]
    [InlineData("-3")]
    public void ParseJobRange_InvalidFormat_ShouldThrow(string input)
    {
        Assert.ThrowsAny<Exception>(() => _parser.ParseJobRange(input));
    }

    [Fact]
    public void ParseJobRange_ReversedRange_ShouldReturnEmpty()
    {
        var result = _parser.ParseJobRange("3-1");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseJobList_ValidList_ShouldReturnSpecifiedIds()
    {
        var result = _parser.ParseJobList("1;3");
        Assert.Equal(new List<int> { 1, 3 }, result);
    }

    [Fact]
    public void ParseJobList_SingleValue_ShouldReturnSingleElement()
    {
        var result = _parser.ParseJobList("5");
        Assert.Equal(new List<int> { 5 }, result);
    }

    [Fact]
    public void ParseJobList_WithWhitespace_ShouldTrimAndParse()
    {
        var result = _parser.ParseJobList(" 1 ; 3 ; 5 ");
        Assert.Equal(new List<int> { 1, 3, 5 }, result);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1;abc;3")]
    public void ParseJobList_InvalidFormat_ShouldThrow(string input)
    {
        Assert.ThrowsAny<Exception>(() => _parser.ParseJobList(input));
    }

    [Fact]
    public void ParseJobList_MultipleItems_ShouldPreserveOrder()
    {
        var result = _parser.ParseJobList("5;2;4;1;3");
        Assert.Equal(new List<int> { 5, 2, 4, 1, 3 }, result);
    }

    // --- ParseInput tests ---

    [Fact]
    public void ParseInput_SingleId_ShouldReturnSingleElement()
    {
        var result = _parser.ParseInput("2");
        Assert.Equal(new List<int> { 2 }, result);
    }

    [Fact]
    public void ParseInput_Range_ShouldReturnSequentialIds()
    {
        var result = _parser.ParseInput("1-3");
        Assert.Equal(new List<int> { 1, 2, 3 }, result);
    }

    [Fact]
    public void ParseInput_SemicolonList_ShouldReturnSpecifiedIds()
    {
        var result = _parser.ParseInput("1;3;5");
        Assert.Equal(new List<int> { 1, 3, 5 }, result);
    }

    [Fact]
    public void ParseInput_WhitespacePadded_ShouldTrimAndParse()
    {
        var result = _parser.ParseInput("  2  ");
        Assert.Equal(new List<int> { 2 }, result);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("--version")]
    [InlineData("-v")]
    [InlineData("abc")]
    [InlineData("1-abc")]
    [InlineData("abc;def")]
    public void ParseInput_InvalidInput_ShouldThrowFormatException(string input)
    {
        var ex = Assert.Throws<FormatException>(() => _parser.ParseInput(input));
        Assert.Contains("Invalid input", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseInput_EmptyOrNull_ShouldThrowFormatException(string? input)
    {
        var ex = Assert.Throws<FormatException>(() => _parser.ParseInput(input!));
        Assert.Contains("Invalid input", ex.Message);
    }

    [Fact]
    public void ParseInput_SemicolonTakesPriorityOverDash()
    {
        // "1;2-3" is routed to ParseJobList (semicolon first), which fails on "2-3"
        var ex = Assert.Throws<FormatException>(() => _parser.ParseInput("1;2-3"));
        Assert.Contains("Invalid input", ex.Message);
    }
}
