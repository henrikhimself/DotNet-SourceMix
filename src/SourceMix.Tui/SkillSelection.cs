namespace Hj.SourceMix.Tui;

internal sealed record SkillSelection(
  IReadOnlyList<string> SelectedKeys,
  IReadOnlySet<string> PinnedKeys)
{
  public StepResult Step { get; init; } = StepResult.Confirm;
}
