namespace Hj.SourceMix.Tui;

internal sealed record FileSelection(
  IReadOnlyList<string> SelectedPaths,
  IReadOnlySet<string> PinnedPaths);
