using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PostHog.Json;

/// <summary>
/// A type that can be either a string or a value of type <typeparamref name="T"/>.
/// When deserializing from JSON, this type can be used to handle cases where a
/// field can be either a string or a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
[JsonConverter(typeof(StringOrValueConverter))]
public readonly struct StringOrValue<T> : IStringOrObject, IEquatable<T>, IEquatable<StringOrValue<T>>
{
    bool IsDefault => !IsValue && !IsString;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringOrValue{T}"/> struct with a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">The value of type <typeparamref name="T"/>.</param>
    public StringOrValue(T value)
    {
        Value = value;
        IsValue = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringOrValue{T}"/> struct with a string value.
    /// </summary>
    /// <param name="stringValue">The string value.</param>
    public StringOrValue(string stringValue)
    {
        StringValue = stringValue;
        IsString = true;
    }

    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string? StringValue { get; }

    /// <summary>
    /// Gets the value of type <typeparamref name="T"/>.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the object value.
    /// </summary>
    object? IStringOrObject.ObjectValue => Value;

    /// <summary>
    /// Gets a value indicating whether this instance is a string.
    /// </summary>
    [MemberNotNullWhen(true, nameof(StringValue))]
    public bool IsString { get; }

    /// <summary>
    /// Gets a value indicating whether this instance is a value.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsValue { get; }

    /// <summary>
    /// Implicitly converts a string to a <see cref="StringOrValue{T}"/>.
    /// </summary>
    /// <param name="stringValue">The string value.</param>
    public static implicit operator StringOrValue<T>(string stringValue) => new(stringValue);

    /// <summary>
    /// Implicitly converts a value of type <typeparamref name="T"/> to a <see cref="StringOrValue{T}"/>.
    /// </summary>
    /// <param name="value">The value of type <typeparamref name="T"/>.</param>
    public static implicit operator StringOrValue<T>(T value) => new(value);

    /// <summary>
    /// Creates a new instance of <see cref="StringOrValue{T}"/> from a string value.
    /// </summary>
    /// <remarks>
    /// This is here to satisfy CA2225: Operator overloads have named alternates.
    /// </remarks>
    public StringOrValue<T> ToStringOrValue() => this;

    public override string ToString() => (IsString ? StringValue : Value?.ToString()) ?? string.Empty;

    public bool Equals(T? obj) => IsValue && obj is not null && EqualityComparer<T>.Default.Equals(Value, obj);

    public bool Equals(StringOrValue<T> other)
        => (IsDefault && other.IsDefault)
           || other.IsValue
           && IsValue
           && EqualityComparer<T>.Default.Equals(Value, other.Value)
           || other.IsString && IsString && StringComparer.Ordinal.Equals(StringValue, other.StringValue);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is StringOrValue<T> value && Equals(value.Value);

    public override int GetHashCode() => IsValue
        ? Value?.GetHashCode() ?? 0
        : StringValue?.GetHashCode(StringComparison.Ordinal) ?? 0;

    // Override the == operator
    public static bool operator ==(StringOrValue<T> left, StringOrValue<T> right)
        => left.Equals(right);

    public static bool operator !=(StringOrValue<T> left, StringOrValue<T> right)
        => !left.Equals(right);

    public static bool operator ==(StringOrValue<T>? left, T right)
        => left is not null && left.Equals(right);

    public static bool operator ==(StringOrValue<T> left, T right)
        => left.Equals(right);

    // Override the != operator
    public static bool operator !=(StringOrValue<T>? left, T right)
        => left is null || !left.Equals(right);

    public static bool operator !=(StringOrValue<T> left, T right)
        => !left.Equals(right);
}

/// <summary>
/// Internal interface for <see cref="StringOrValue{T}"/>.
/// </summary>
/// <remarks>
/// This is here to make serialization and deserialization easy.
/// </remarks>
[JsonConverter(typeof(StringOrValueConverter))]
internal interface IStringOrObject
{
    bool IsString { get; }

    bool IsValue { get; }

    string? StringValue { get; }

    object? ObjectValue { get; }
}

/// <summary>
/// Json converter for <see cref="StringOrValue{T}"/>. Only <see cref="bool"/> and <see cref="int"/> are
/// supported. Reads and writes go through <see cref="Utf8JsonReader"/>/<see cref="Utf8JsonWriter"/>
/// directly to stay NativeAOT-safe (no generic <c>JsonSerializer</c> round-trip).
/// </summary>
internal class StringOrValueConverter : JsonConverter<IStringOrObject>
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType
           && typeToConvert.GetGenericTypeDefinition() == typeof(StringOrValue<>);

    public override IStringOrObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var targetType = typeToConvert.GetGenericArguments()[0];

        if (targetType == typeof(bool))
        {
            return ReadBool(ref reader);
        }

        if (targetType == typeof(int))
        {
            return ReadInt(ref reader);
        }

        throw new NotSupportedException(
            $"StringOrValue<{targetType.Name}> is not supported.");
    }

    static StringOrValue<bool> ReadBool(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            return s is null ? default : new StringOrValue<bool>(s);
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        return new StringOrValue<bool>(reader.GetBoolean());
    }

    static StringOrValue<int> ReadInt(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            return s is null ? default : new StringOrValue<int>(s);
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        return new StringOrValue<int>(reader.GetInt32());
    }

    public override void Write(Utf8JsonWriter writer, IStringOrObject value, JsonSerializerOptions options)
    {
        if (value.IsString)
        {
            writer.WriteStringValue(value.StringValue);
            return;
        }

        if (!value.IsValue)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value.ObjectValue)
        {
            case bool b:
                writer.WriteBooleanValue(b);
                return;
            case int i:
                writer.WriteNumberValue(i);
                return;
            default:
                throw new NotSupportedException(
                    $"StringOrValue<{value.ObjectValue?.GetType().Name}> is not supported.");
        }
    }
}
