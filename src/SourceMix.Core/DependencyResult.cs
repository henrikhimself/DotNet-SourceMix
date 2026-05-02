namespace Hj.SourceMix.Core;

public sealed record DependencyResult(
  IReadOnlyList<string> Files,
  IReadOnlySet<string> UnresolvedTypeNames);
