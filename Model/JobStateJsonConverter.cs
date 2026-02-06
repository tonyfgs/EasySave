using System.Text.Json;
using System.Text.Json.Serialization;

namespace Model;

public class JobStateJsonConverter : JsonConverter<JobState>
{
    public override JobState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "ACTIVE" => JobState.Active,
            "INACTIVE" => JobState.Inactive,
            "END" => JobState.End,
            "ERROR" => JobState.Error,
            _ => throw new JsonException($"Unknown JobState value: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, JobState value, JsonSerializerOptions options)
    {
        var text = value switch
        {
            JobState.Active => "ACTIVE",
            JobState.Inactive => "INACTIVE",
            JobState.End => "END",
            JobState.Error => "ERROR",
            _ => throw new JsonException($"Unknown JobState value: {value}")
        };
        writer.WriteStringValue(text);
    }
}
