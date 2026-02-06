using System.Text.Json.Serialization;

namespace Model;

[JsonConverter(typeof(JobStateJsonConverter))]
public enum JobState
{
    Inactive,
    Active,
    End,
    Error
}
