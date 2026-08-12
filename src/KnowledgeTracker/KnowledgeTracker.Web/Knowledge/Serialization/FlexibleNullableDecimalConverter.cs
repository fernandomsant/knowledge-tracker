using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KnowledgeTracker.Web.Knowledge.Serialization;

public sealed class FlexibleNullableDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var number)) return number;
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant)) return invariant;
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out var portuguese)) return portuguese;
        }

        throw new JsonException("Target value must be a valid decimal number.");
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteNumberValue(value.Value);
    }
}
