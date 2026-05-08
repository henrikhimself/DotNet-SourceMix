namespace Hj.SourceMix.Core;

public sealed record PreferenceDefaults
{
  public bool Recursive { get; init; }

  public bool LimitDepth { get; init; }

  public int MaxDepth { get; init; } = 3;

  public bool IncludeCompiled { get; init; }

  public bool Trim { get; init; }

  public bool ExpandTypes { get; init; }
}
