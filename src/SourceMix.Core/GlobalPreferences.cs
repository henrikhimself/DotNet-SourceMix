using System.Text.Json.Serialization;

namespace Hj.SourceMix.Core;

public sealed record GlobalPreferences
{
  public Dictionary<string, string> CustomPrompts { get; init; } = [];
}

[JsonSerializable(typeof(GlobalPreferences))]
[JsonSourceGenerationOptions(
  WriteIndented = true,
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class GlobalPreferencesJsonContext : JsonSerializerContext;
