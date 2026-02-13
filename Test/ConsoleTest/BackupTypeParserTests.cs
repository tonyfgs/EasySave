using EasySave.Utilities;
using Model;

namespace ConsoleTest;

public class BackupTypeParserTests
{
    [Theory]
    [InlineData("Full", BackupType.Full)]
    [InlineData("full", BackupType.Full)]
    [InlineData("FULL", BackupType.Full)]
    [InlineData("Differential", BackupType.Differential)]
    [InlineData("differential", BackupType.Differential)]
    [InlineData("DIFFERENTIAL", BackupType.Differential)]
    public void Parse_ValidName_ShouldReturnBackupType(string input, BackupType expected)
    {
        var result = BackupTypeParser.Parse(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("-1")]
    [InlineData("99")]
    public void Parse_NumericString_ShouldThrowArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => BackupTypeParser.Parse(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Incremental")]
    [InlineData("abc")]
    public void Parse_InvalidName_ShouldThrowArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => BackupTypeParser.Parse(input));
    }
}
