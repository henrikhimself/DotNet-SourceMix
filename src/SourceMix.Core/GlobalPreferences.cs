namespace Hj.SourceMix.Core;

public sealed record GlobalPreferences
{
  public Dictionary<string, string> CustomPrompts { get; init; } = [];
}
