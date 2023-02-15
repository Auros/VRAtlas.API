using System.Text.Json;
using System.Text.Json.Serialization;
using VRAtlas.Models;

namespace VRAtlas.Converters;

public class WritableTagModelJsonConverter : JsonConverter<EventTag>
{
    public override EventTag? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, EventTag value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Tag.Name);
    }
}