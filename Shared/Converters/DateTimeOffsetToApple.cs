using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Converters;

public class DateTimeOffsetToApple: JsonConverter<DateTimeOffset>
{
    private static readonly DateTimeOffset Origin = new(2001, 1, 1, 1, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UnixTime = new(1970, 1, 1, 1, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            var since = reader.GetDouble();
            return since > 1500000000 ? UnixTime.AddSeconds(since) : Origin.AddSeconds(since);
        }
        catch
        {
            return DateTimeOffset.MinValue;
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToUnixTimeSeconds() - Origin.ToUnixTimeSeconds());
    }
}

public static class StringExtensions
{
    public static DateTimeOffset AppleTime(this string appleEpoch)
    {
        int.TryParse(appleEpoch, out var since);
        return new DateTimeOffset(2001, 1, 1, 1, 0, 0, TimeSpan.Zero).AddSeconds(since);
    }
}