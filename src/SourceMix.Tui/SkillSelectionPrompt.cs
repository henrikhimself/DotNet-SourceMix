using Hj.SourceMix.Core;
using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal sealed record SkillSelection(
  IReadOnlyList<string> SelectedKeys,
  IReadOnlySet<string> PinnedKeys);

internal static class SkillSelectionPrompt
{
  private const int MaxVisible = 20;

  internal static SkillSelection Show(IReadOnlyList<Skill> skills, IReadOnlySet<string> pinnedKeys)
  {
    var selected = new HashSet<string>(pinnedKeys, StringComparer.OrdinalIgnoreCase);
    var pinned = new HashSet<string>(pinnedKeys, StringComparer.OrdinalIgnoreCase);
    var cursor = 0;
    var scroll = 0;

    while (true)
    {
      if (cursor >= skills.Count && skills.Count > 0)
      {
        cursor = skills.Count - 1;
      }

      var renderedLines = Render(skills, selected, pinned, cursor, scroll);
      var key = Console.ReadKey(intercept: true);
      ClearLines(renderedLines);

      switch (key.Key)
      {
        case ConsoleKey.Enter:
          AnsiConsole.WriteLine();
          return new SkillSelection([.. selected], pinned);

        case ConsoleKey.Escape:
          AnsiConsole.WriteLine();
          return new SkillSelection([], new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        case ConsoleKey.UpArrow:
          if (cursor > 0)
          {
            cursor--;

            if (cursor < scroll)
            {
              scroll = cursor;
            }
          }

          break;

        case ConsoleKey.DownArrow:
          if (cursor < skills.Count - 1)
          {
            cursor++;

            if (cursor >= scroll + MaxVisible)
            {
              scroll = cursor - MaxVisible + 1;
            }
          }

          break;

        case ConsoleKey.Spacebar:
          if (skills.Count > 0)
          {
            var skill = skills[cursor];

            if (!selected.Remove(skill.Key))
            {
              selected.Add(skill.Key);
            }

            pinned.Remove(skill.Key);
          }

          break;

        case ConsoleKey.P when key.Modifiers.HasFlag(ConsoleModifiers.Control):
          if (skills.Count > 0)
          {
            var skill = skills[cursor];

            if (pinned.Remove(skill.Key))
            {
              selected.Remove(skill.Key);
            }
            else
            {
              pinned.Add(skill.Key);
              selected.Add(skill.Key);
            }
          }

          break;
      }
    }
  }

  private static int Render(
    IReadOnlyList<Skill> skills,
    HashSet<string> selected,
    HashSet<string> pinned,
    int cursorIndex,
    int scrollOffset)
  {
    AnsiConsole.MarkupLine(
      $"[bold]Select skills[/]  [dim]({selected.Count} selected · {pinned.Count} pinned)[/]  " +
      "[dim](Space select · Ctrl+P pin · Enter confirm)[/]");
    AnsiConsole.WriteLine();

    if (skills.Count == 0)
    {
      AnsiConsole.MarkupLine("  [dim]No skills found.[/]");

      return 4;
    }

    var visibleEnd = Math.Min(scrollOffset + MaxVisible, skills.Count);

    for (var i = scrollOffset; i < visibleEnd; i++)
    {
      var skill = skills[i];
      var isSelected = selected.Contains(skill.Key);
      var isPinned = pinned.Contains(skill.Key);
      var isCursor = i == cursorIndex;

      var checkbox = isSelected ? "[green]+[/]" : "[dim]-[/]";
      var arrow = isCursor ? "[darkorange]>[/]" : " ";
      var pin = isPinned ? " [cyan]*[/]" : string.Empty;

      if (isCursor)
      {
        AnsiConsole.MarkupLine($" {arrow} {checkbox} [bold]{Markup.Escape(skill.Key)}[/]{pin}");
      }
      else
      {
        AnsiConsole.MarkupLine($" {arrow} {checkbox} [dim]{Markup.Escape(skill.Key)}[/]{pin}");
      }
    }

    var extraLines = 0;

    if (skills.Count > MaxVisible)
    {
      var remaining = skills.Count - visibleEnd;

      if (remaining > 0)
      {
        AnsiConsole.MarkupLine($"  [dim]... {remaining} more (scroll down)[/]");
        extraLines++;
      }
    }

    return 3 + (visibleEnd - scrollOffset) + extraLines;
  }

  private static void ClearLines(int lineCount)
  {
    for (var i = 0; i < lineCount; i++)
    {
      Console.CursorLeft = 0;
      Console.Write(new string(' ', Console.WindowWidth > 0 ? Console.WindowWidth : 80));
      Console.CursorLeft = 0;

      if (i < lineCount - 1)
      {
        Console.CursorTop--;
      }
    }
  }
}
