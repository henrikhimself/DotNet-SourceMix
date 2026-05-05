using Hj.SourceMix.Core;
using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal static class SkillSelectionPrompt
{
  internal static SkillSelection Show(
    IReadOnlyList<Skill> skills,
    IReadOnlySet<string> pinnedKeys,
    ITuiConsole? tuiConsole = null,
    IKeyReader? keys = null)
  {
    ArgumentNullException.ThrowIfNull(skills);
    ArgumentNullException.ThrowIfNull(pinnedKeys);

    tuiConsole ??= new SystemTuiConsole();
    keys ??= new ConsoleKeyReader();

    var initial = new HashSet<string>(pinnedKeys, StringComparer.OrdinalIgnoreCase);

    var picker = new ListPickerPrompt<Skill>
    {
      Header = "[bold]Select skills[/]  [dim](Space select \u00b7 Ctrl+P pin/unpin \u00b7 Enter confirm \u00b7 Ctrl+Q back)[/]",
      Items = skills,
      KeySelector = static s => s.Key,
      MultiSelect = true,
      AllowPin = true,
      LinkPinAndSelection = true,
      InitialSelected = initial,
      InitialPinned = initial,
      Renderer = RenderRow,
    };

    var result = picker.Show(tuiConsole, keys);

    if (result.Reason == ListPickerExitReason.Skipped)
    {
      return new SkillSelection([], new HashSet<string>(StringComparer.OrdinalIgnoreCase))
      {
        Step = StepResult.Back,
      };
    }

    if (result.Reason == ListPickerExitReason.Quit)
    {
      return new SkillSelection([], new HashSet<string>(StringComparer.OrdinalIgnoreCase))
      {
        Step = StepResult.Quit,
      };
    }

    return new SkillSelection(
      [.. result.Selected.Select(s => s.Key)],
      result.Pinned);
  }

  private static string RenderRow(Skill skill, ListPickerItemState state)
  {
    var arrow = state.IsCursor ? "[darkorange]>[/]" : " ";
    var checkbox = state.IsSelected ? "[green]+[/]" : "[dim]-[/]";
    var pin = state.IsPinned ? " [cyan]*[/]" : string.Empty;
    var label = state.IsCursor
      ? $"[bold]{Markup.Escape(skill.Key)}[/]"
      : $"[dim]{Markup.Escape(skill.Key)}[/]";

    return $" {arrow} {checkbox} {label}{pin}";
  }
}
