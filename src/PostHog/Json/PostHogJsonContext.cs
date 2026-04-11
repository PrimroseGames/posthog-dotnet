using System.Text.Json;
using System.Text.Json.Serialization;
using PostHog.Api;
using PostHog.Features;

namespace PostHog.Json;

/// <summary>
/// Source-generated JSON metadata for every type the SDK serializes or deserializes. Used to keep the
/// library NativeAOT-compatible — all <c>JsonSerializer</c> calls in <c>src/PostHog</c> go through one of
/// the typed <c>JsonTypeInfo&lt;T&gt;</c> handles exposed here.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// Response DTOs.
[JsonSerializable(typeof(LocalEvaluationApiResult))]
[JsonSerializable(typeof(LocalFeatureFlag))]
[JsonSerializable(typeof(FeatureFlagFilters))]
[JsonSerializable(typeof(FeatureFlagGroup))]
[JsonSerializable(typeof(Multivariate))]
[JsonSerializable(typeof(Variant))]
[JsonSerializable(typeof(Filter))]
[JsonSerializable(typeof(FilterSet))]
[JsonSerializable(typeof(PropertyFilter))]
[JsonSerializable(typeof(PropertyFilterValue))]
[JsonSerializable(typeof(FlagsApiResult))]
[JsonSerializable(typeof(FeatureFlagResult))]
[JsonSerializable(typeof(FeatureFlagMetadata))]
[JsonSerializable(typeof(EvaluationReason))]
[JsonSerializable(typeof(FeatureFlag))]
[JsonSerializable(typeof(FeatureFlagWithMetadata))]
[JsonSerializable(typeof(ApiResult))]
[JsonSerializable(typeof(ApiErrorResult))]
[JsonSerializable(typeof(JsonDocument))]
// Request payloads.
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(CapturedEvent))]
// Enums.
[JsonSerializable(typeof(FilterType))]
[JsonSerializable(typeof(ComparisonOperator))]
// Collections threaded through the read-only collection/dictionary factories.
[JsonSerializable(typeof(List<LocalFeatureFlag>))]
[JsonSerializable(typeof(List<FeatureFlagGroup>))]
[JsonSerializable(typeof(List<PropertyFilter>))]
[JsonSerializable(typeof(List<Filter>))]
[JsonSerializable(typeof(List<Variant>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, StringOrValue<bool>>))]
[JsonSerializable(typeof(Dictionary<string, FilterSet>))]
[JsonSerializable(typeof(Dictionary<string, FeatureFlagResult>))]
[JsonSerializable(typeof(Dictionary<string, FeatureFlag>))]
// Cache-key helper.
[JsonSerializable(typeof(KeyValuePair<string, object?>[]))]
internal partial class PostHogJsonContext : JsonSerializerContext;
