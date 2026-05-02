namespace Hj.SourceMix.Tui;

internal sealed record SkillSelection(
  IReadOnlyList<string> SelectedKeys,
  IReadOnlySet<string> PinnedKeys);
