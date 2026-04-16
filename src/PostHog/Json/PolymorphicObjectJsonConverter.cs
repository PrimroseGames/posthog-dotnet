using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using PostHog.Api;

namespace PostHog.Json;

/// <summary>
/// AOT-safe <see cref="JsonConverter{T}"/> for <see cref="object"/> values. Registered on
/// <see cref="JsonSerializerHelper.Options"/> so that every <c>object</c>-typed slot in the SDK
/// (most notably <c>Dictionary&lt;string, object&gt;</c> event property bags) writes through a
/// bounded, reflection-free switch instead of STJ's default polymorphic writer — which carries
/// <c>RequiresDynamicCode</c>.
/// </summary>
internal sealed class PolymorphicObjectJsonConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // The SDK only serializes open-polymorphic values, never deserializes them. If we ever hit
        // this path, fall back to a JsonElement so callers can inspect the shape without reflection.
        return JsonElement.ParseValue(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        WriteValue(writer, value);
    }

    static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                return;
            case string s:
                writer.WriteStringValue(s);
                return;
            case bool b:
                writer.WriteBooleanValue(b);
                return;
            case int i:
                writer.WriteNumberValue(i);
                return;
            case long l:
                writer.WriteNumberValue(l);
                return;
            case double d:
                writer.WriteNumberValue(d);
                return;
            case float f:
                writer.WriteNumberValue(f);
                return;
            case decimal m:
                writer.WriteNumberValue(m);
                return;
            case short sh:
                writer.WriteNumberValue(sh);
                return;
            case ushort us:
                writer.WriteNumberValue(us);
                return;
            case uint ui:
                writer.WriteNumberValue(ui);
                return;
            case ulong ul:
                writer.WriteNumberValue(ul);
                return;
            case byte by:
                writer.WriteNumberValue(by);
                return;
            case sbyte sb:
                writer.WriteNumberValue(sb);
                return;
            case DateTime dt:
                writer.WriteStringValue(dt);
                return;
            case DateTimeOffset dto:
                writer.WriteStringValue(dto);
                return;
            case Guid g:
                writer.WriteStringValue(g);
                return;
            case DateOnly d2:
                writer.WriteStringValue(d2.ToString("O", CultureInfo.InvariantCulture));
                return;
            case TimeOnly t:
                writer.WriteStringValue(t.ToString("O", CultureInfo.InvariantCulture));
                return;
            case Uri uri:
                writer.WriteStringValue(uri.ToString());
                return;
            case Enum e:
                writer.WriteStringValue(e.ToString());
                return;
            case JsonElement je:
                je.WriteTo(writer);
                return;
            case JsonDocument jd:
                jd.RootElement.WriteTo(writer);
                return;
            case IStringOrObject sov:
                // Delegate to the existing converter's non-reflective write path.
                if (sov.IsString)
                {
                    writer.WriteStringValue(sov.StringValue);
                }
                else if (sov.IsValue)
                {
                    WriteValue(writer, sov.ObjectValue);
                }
                else
                {
                    writer.WriteNullValue();
                }
                return;
            case CapturedEvent captured:
                JsonSerializer.Serialize(writer, captured, JsonSerializerHelper.GetTypeInfo<CapturedEvent>());
                return;
        }

        // Handle dictionaries before enumerables — IDictionary also implements IEnumerable.
        if (value is IReadOnlyDictionary<string, object?> roDictNullable)
        {
            writer.WriteStartObject();
            foreach (var kvp in roDictNullable)
            {
                writer.WritePropertyName(kvp.Key);
                WriteValue(writer, kvp.Value);
            }
            writer.WriteEndObject();
            return;
        }

        if (value is IReadOnlyDictionary<string, object> roDict)
        {
            writer.WriteStartObject();
            foreach (var kvp in roDict)
            {
                writer.WritePropertyName(kvp.Key);
                WriteValue(writer, kvp.Value);
            }
            writer.WriteEndObject();
            return;
        }

        if (value is IDictionary<string, object?> dictNullable)
        {
            writer.WriteStartObject();
            foreach (var kvp in dictNullable)
            {
                writer.WritePropertyName(kvp.Key);
                WriteValue(writer, kvp.Value);
            }
            writer.WriteEndObject();
            return;
        }

        if (value is IDictionary<string, object> dict)
        {
            writer.WriteStartObject();
            foreach (var kvp in dict)
            {
                writer.WritePropertyName(kvp.Key);
                WriteValue(writer, kvp.Value);
            }
            writer.WriteEndObject();
            return;
        }

        if (value is IEnumerable enumerable)
        {
            writer.WriteStartArray();
            foreach (var item in enumerable)
            {
                WriteValue(writer, item);
            }
            writer.WriteEndArray();
            return;
        }

        throw new NotSupportedException(
            $"PolymorphicObjectJsonConverter cannot serialize value of type '{value.GetType().FullName}'. "
            + "Register a dedicated converter or add the type to PostHogJsonContext.");
    }
}
