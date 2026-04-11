using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace PostHog.Json;

internal static class JsonSerializerHelper
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = PostHogJsonContext.Default,
        Converters =
        {
            new PolymorphicObjectJsonConverter(),
            new ReadOnlyCollectionJsonConverterFactory(),
            new ReadOnlyDictionaryJsonConverterFactory()
        }
    };

    static readonly JsonSerializerOptions IndentedOptions = new(Options)
    {
        WriteIndented = true
    };

    public static async Task<string> SerializeToCamelCaseJsonStringAsync<T>(
        T obj,
        JsonTypeInfo<T> typeInfo,
        bool writeIndented = false)
    {
        var stream = await SerializeToCamelCaseJsonStreamAsync(obj, typeInfo, writeIndented);
        stream.Position = 0;
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    static async Task<Stream> SerializeToCamelCaseJsonStreamAsync<T>(
        T obj,
        JsonTypeInfo<T> typeInfo,
        bool writeIndented = false)
    {
        // Source-gen contexts are built against one specific JsonSerializerOptions. Re-use the type
        // info's own options so WriteIndented stays honored without creating a second context.
        var stream = new MemoryStream();
        if (writeIndented && !typeInfo.Options.WriteIndented)
        {
            // Fall back to the shared indented options by looking up the same type in them.
            var indentedInfo = (JsonTypeInfo<T>)IndentedOptions.GetTypeInfo(typeof(T));
            await JsonSerializer.SerializeAsync(stream, obj, indentedInfo);
        }
        else
        {
            await JsonSerializer.SerializeAsync(stream, obj, typeInfo);
        }
        return stream;
    }

    public static async Task<T?> DeserializeFromCamelCaseJsonStringAsync<T>(
        string json,
        JsonTypeInfo<T> typeInfo)
    {
        using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        jsonStream.Position = 0;
        return await DeserializeFromCamelCaseJsonAsync(jsonStream, typeInfo);
    }

    public static async Task<T?> DeserializeFromCamelCaseJsonAsync<T>(
        Stream jsonStream,
        JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken = default) =>
        await JsonSerializer.DeserializeAsync(jsonStream, typeInfo, cancellationToken);

    // ------------------------------------------------------------------------------------------------
    // Legacy reflection-based overloads. Retained for the unit-test suite, which is not AOT compiled.
    // Production code paths must call the typed <see cref="JsonTypeInfo{T}"/> overloads above.
    // ------------------------------------------------------------------------------------------------

    const string ReflectionWarning =
        "Uses reflection-based JSON serialization. Callers must opt-in (tests only). "
        + "Library code should use the JsonTypeInfo<T> overloads exposed on PostHogJsonContext.Default.";

    [RequiresUnreferencedCode(ReflectionWarning)]
    [RequiresDynamicCode(ReflectionWarning)]
    public static async Task<string> SerializeToCamelCaseJsonStringAsync<T>(T obj, bool writeIndented = false)
    {
        var options = writeIndented ? IndentedOptions : Options;
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, obj, options);
        stream.Position = 0;
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    [RequiresUnreferencedCode(ReflectionWarning)]
    [RequiresDynamicCode(ReflectionWarning)]
    public static async Task<T?> DeserializeFromCamelCaseJsonStringAsync<T>(string json)
    {
        using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        jsonStream.Position = 0;
        return await JsonSerializer.DeserializeAsync<T>(jsonStream, Options);
    }
}
