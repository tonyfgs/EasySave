using System.Text.Json.Serialization;

namespace Model;

[JsonConverter(typeof(JobStateJsonConverter))]
public enum JobState
{
    Paused,
    Stopping,
    Inactive,
    Active,
    End,
    Error,
    Blocked
}
