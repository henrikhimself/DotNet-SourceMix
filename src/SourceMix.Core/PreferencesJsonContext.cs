using System.Text.Json.Serialization;

namespace Hj.SourceMix.Core;

[JsonSerializable(typeof(SolutionPreferences))]
[JsonSourceGenerationOptions(
  WriteIndented = true,
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class PreferencesJsonContext : JsonSerializerContext;
