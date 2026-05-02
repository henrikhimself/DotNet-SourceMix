namespace Hj.SourceMix.Core;

public sealed record SolutionPreferences
{
  public string SolutionPath { get; init; } = string.Empty;

  public List<string> PinnedFiles { get; init; } = [];

  public string? OutputPath { get; init; }

  public PreferenceDefaults Defaults { get; init; } = new();

  public string? DefaultPromptKey { get; init; }

  public List<string> PinnedSkills { get; init; } = [];
}
