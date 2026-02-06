using System.Text.Json;
using Model;

namespace ModelTest;

public class JobStateSerializationTests
{
    [Theory]
    [InlineData(JobState.Active, "\"ACTIVE\"")]
    [InlineData(JobState.Inactive, "\"INACTIVE\"")]
    [InlineData(JobState.End, "\"END\"")]
    [InlineData(JobState.Error, "\"ERROR\"")]
    public void JobState_ShouldSerializeAsUppercase(JobState state, string expected)
    {
        var json = JsonSerializer.Serialize(state);
        Assert.Equal(expected, json);
    }

    [Theory]
    [InlineData("\"ACTIVE\"", JobState.Active)]
    [InlineData("\"INACTIVE\"", JobState.Inactive)]
    [InlineData("\"END\"", JobState.End)]
    [InlineData("\"ERROR\"", JobState.Error)]
    public void JobState_ShouldDeserializeFromUppercase(string json, JobState expected)
    {
        var result = JsonSerializer.Deserialize<JobState>(json);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void JobState_UnknownString_ShouldThrowJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JobState>("\"UNKNOWN\""));
    }
}
