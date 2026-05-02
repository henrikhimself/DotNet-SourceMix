using System.Text.Json.Serialization;

namespace Hj.SourceMix.Core;

[JsonSerializable(typeof(GlobalPreferences))]
[JsonSourceGenerationOptions(
  WriteIndented = true,
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class GlobalPreferencesJsonContext : JsonSerializerContext;
