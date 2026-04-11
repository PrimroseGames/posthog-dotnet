using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PostHog.Api;
using PostHog.Features;

namespace PostHog.Json;

/// <summary>
/// Wraps deserialized <see cref="Dictionary{TKey, TValue}"/> values as <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
/// Threads a source-generated <see cref="JsonTypeInfo{T}"/> through the converter so no generic
/// <c>JsonSerializer.Serialize/Deserialize&lt;T&gt;</c> calls remain at runtime — which keeps NativeAOT happy.
/// </summary>
internal sealed class ReadOnlyDictionaryJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        var genericTypeDefinition = typeToConvert.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(IReadOnlyDictionary<,>)
               || genericTypeDefinition == typeof(ReadOnlyDictionary<,>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var keyType = typeToConvert.GetGenericArguments()[0];
        var valueType = typeToConvert.GetGenericArguments()[1];
        var context = PostHogJsonContext.Default;

        if (keyType == typeof(string))
        {
            if (valueType == typeof(string))
                return new ReadonlyDictionaryJsonConverter<string, string>(context.DictionaryStringString);
            if (valueType == typeof(StringOrValue<bool>))
                return new ReadonlyDictionaryJsonConverter<string, StringOrValue<bool>>(context.DictionaryStringStringOrValueBoolean);
            if (valueType == typeof(FilterSet))
                return new ReadonlyDictionaryJsonConverter<string, FilterSet>(context.DictionaryStringFilterSet);
            if (valueType == typeof(FeatureFlagResult))
                return new ReadonlyDictionaryJsonConverter<string, FeatureFlagResult>(context.DictionaryStringFeatureFlagResult);
            if (valueType == typeof(FeatureFlag))
                return new ReadonlyDictionaryJsonConverter<string, FeatureFlag>(context.DictionaryStringFeatureFlag);
        }

        throw new NotSupportedException(
            $"ReadOnlyDictionary<{keyType.Name}, {valueType.Name}> is not supported. "
            + "Register Dictionary<K,V> in PostHogJsonContext and add a factory branch.");
    }
}

internal sealed class ReadonlyDictionaryJsonConverter<TKey, TValue> : JsonConverter<IReadOnlyDictionary<TKey, TValue>>
    where TKey : notnull
{
    readonly JsonTypeInfo<Dictionary<TKey, TValue>> _dictionaryTypeInfo;

    public ReadonlyDictionaryJsonConverter(JsonTypeInfo<Dictionary<TKey, TValue>> dictionaryTypeInfo)
    {
        _dictionaryTypeInfo = dictionaryTypeInfo;
    }

    public override IReadOnlyDictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dictionary = JsonSerializer.Deserialize(ref reader, _dictionaryTypeInfo);
        return dictionary is null ? null : new ReadOnlyDictionary<TKey, TValue>(dictionary);
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<TKey, TValue> value, JsonSerializerOptions options)
    {
        var dictionary = value as Dictionary<TKey, TValue> ?? new Dictionary<TKey, TValue>(value);
        JsonSerializer.Serialize(writer, dictionary, _dictionaryTypeInfo);
    }
}
