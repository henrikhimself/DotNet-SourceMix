using System.Text.Json.Serialization;

namespace Hj.SourceMix.Core;

public sealed record SolutionPreferences
{
  public string SolutionPath { get; init; } = string.Empty;

  public List<string> PinnedFiles { get; init; } = [];

  public string? OutputPath { get; init; }

  public PreferenceDefaults Defaults { get; init; } = new();
}

public sealed record PreferenceDefaults
{
  public bool Recursive { get; init; }

  public bool LimitDepth { get; init; }

  public int MaxDepth { get; init; } = 3;

  public bool IncludeCompiled { get; init; }

  public bool Trim { get; init; }
}

[JsonSerializable(typeof(SolutionPreferences))]
[JsonSourceGenerationOptions(
  WriteIndented = true,
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class PreferencesJsonContext : JsonSerializerContext;
