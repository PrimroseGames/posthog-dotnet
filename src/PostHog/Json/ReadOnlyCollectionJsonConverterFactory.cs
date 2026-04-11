using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PostHog.Api;

namespace PostHog.Json;

/// <summary>
/// Wraps deserialized <see cref="List{T}"/> values as <see cref="ReadOnlyCollection{T}"/>. Threads a
/// source-generated <see cref="JsonTypeInfo{T}"/> through the converter so no generic
/// <c>JsonSerializer.Serialize/Deserialize&lt;T&gt;</c> calls remain at runtime — which keeps NativeAOT happy.
/// </summary>
internal sealed class ReadOnlyCollectionJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        var genericTypeDefinition = typeToConvert.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(IReadOnlyCollection<>)
            || genericTypeDefinition == typeof(ReadOnlyCollection<>)
            || genericTypeDefinition == typeof(IReadOnlyList<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var context = PostHogJsonContext.Default;

        if (elementType == typeof(LocalFeatureFlag))
            return new ReadOnlyCollectionJsonConverter<LocalFeatureFlag>(context.ListLocalFeatureFlag);
        if (elementType == typeof(FeatureFlagGroup))
            return new ReadOnlyCollectionJsonConverter<FeatureFlagGroup>(context.ListFeatureFlagGroup);
        if (elementType == typeof(PropertyFilter))
            return new ReadOnlyCollectionJsonConverter<PropertyFilter>(context.ListPropertyFilter);
        if (elementType == typeof(Filter))
            return new ReadOnlyCollectionJsonConverter<Filter>(context.ListFilter);
        if (elementType == typeof(Variant))
            return new ReadOnlyCollectionJsonConverter<Variant>(context.ListVariant);
        if (elementType == typeof(string))
            return new ReadOnlyCollectionJsonConverter<string>(context.ListString);

        throw new NotSupportedException(
            $"ReadOnlyCollection<{elementType.Name}> is not supported. "
            + "Register List<T> in PostHogJsonContext and add a factory branch.");
    }
}

internal sealed class ReadOnlyCollectionJsonConverter<T> : JsonConverter<IEnumerable<T>>
{
    readonly JsonTypeInfo<List<T>> _listTypeInfo;

    public ReadOnlyCollectionJsonConverter(JsonTypeInfo<List<T>> listTypeInfo)
    {
        _listTypeInfo = listTypeInfo;
    }

    public override IEnumerable<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize(ref reader, _listTypeInfo);
        return list is null ? null : new ReadOnlyCollection<T>(list);
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<T> value, JsonSerializerOptions options)
    {
        var list = value as List<T> ?? [.. value];
        JsonSerializer.Serialize(writer, list, _listTypeInfo);
    }
}
