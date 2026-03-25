using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal sealed record FileSelection(
  IReadOnlyList<string> SelectedPaths,
  IReadOnlySet<string> PinnedPaths);

internal static class FileSearchPrompt
{
  private const int MaxVisible = 20;

  private enum ViewMode { Search, Pinned, Selected }

  internal static FileSelection Show(IReadOnlyList<CsFile> files, IReadOnlySet<string> pinnedPaths)
  {
    var selected = new HashSet<string>(pinnedPaths, StringComparer.OrdinalIgnoreCase);
    var pinned = new HashSet<string>(pinnedPaths, StringComparer.OrdinalIgnoreCase);

    var view = ViewMode.Search;
    var search = string.Empty;
    var searchCursor = 0;
    var searchScroll = 0;
    var pinnedCursor = 0;
    var pinnedScroll = 0;
    var selectedCursor = 0;
    var selectedScroll = 0;

    while (true)
    {
      var filtered = Filter(files, search);

      if (searchCursor >= filtered.Count && filtered.Count > 0)
      {
        searchCursor = filtered.Count - 1;
      }

      var pinnedList = GetPinnedList(files, pinned);

      if (pinnedCursor >= pinnedList.Count && pinnedList.Count > 0)
      {
        pinnedCursor = pinnedList.Count - 1;
      }

      var selectedList = GetSelectedList(files, selected);

      if (selectedCursor >= selectedList.Count && selectedList.Count > 0)
      {
        selectedCursor = selectedList.Count - 1;
      }

      var tokenEstimate = FormatTokenEstimate(files, selected);

      var renderedLines = view switch
      {
        ViewMode.Search => RenderSearch(search, filtered, selected, pinned, searchCursor, searchScroll, tokenEstimate),
        ViewMode.Pinned => RenderPinned(pinnedList, selected, pinned, pinnedCursor, pinnedScroll, tokenEstimate),
        _ => RenderSelected(selectedList, selected, pinned, selectedCursor, selectedScroll, tokenEstimate),
      };

      var key = Console.ReadKey(intercept: true);

      ClearLines(renderedLines);

      switch (key.Key)
      {
        case ConsoleKey.Enter:
          AnsiConsole.WriteLine();
          return new FileSelection([.. selected], pinned);

        case ConsoleKey.Escape:
          AnsiConsole.WriteLine();
          return new FileSelection([], new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        case ConsoleKey.Tab:
          view = view switch
          {
            ViewMode.Search => ViewMode.Pinned,
            ViewMode.Pinned => ViewMode.Selected,
            _ => ViewMode.Search,
          };
          break;

        case ConsoleKey.UpArrow:
          if (view == ViewMode.Search)
          {
            if (searchCursor > 0)
            {
              searchCursor--;

              if (searchCursor < searchScroll)
              {
                searchScroll = searchCursor;
              }
            }
          }
          else if (view == ViewMode.Pinned)
          {
            if (pinnedCursor > 0)
            {
              pinnedCursor--;

              if (pinnedCursor < pinnedScroll)
              {
                pinnedScroll = pinnedCursor;
              }
            }
          }
          else
          {
            if (selectedCursor > 0)
            {
              selectedCursor--;

              if (selectedCursor < selectedScroll)
              {
                selectedScroll = selectedCursor;
              }
            }
          }

          break;

        case ConsoleKey.DownArrow:
          if (view == ViewMode.Search)
          {
            if (searchCursor < filtered.Count - 1)
            {
              searchCursor++;

              if (searchCursor >= searchScroll + MaxVisible)
              {
                searchScroll = searchCursor - MaxVisible + 1;
              }
            }
          }
          else if (view == ViewMode.Pinned)
          {
            if (pinnedCursor < pinnedList.Count - 1)
            {
              pinnedCursor++;

              if (pinnedCursor >= pinnedScroll + MaxVisible)
              {
                pinnedScroll = pinnedCursor - MaxVisible + 1;
              }
            }
          }
          else
          {
            if (selectedCursor < selectedList.Count - 1)
            {
              selectedCursor++;

              if (selectedCursor >= selectedScroll + MaxVisible)
              {
                selectedScroll = selectedCursor - MaxVisible + 1;
              }
            }
          }

          break;

        case ConsoleKey.Spacebar:
          if (view == ViewMode.Search)
          {
            if (filtered.Count > 0)
            {
              var file = filtered[searchCursor];

              if (!selected.Remove(file.FullPath))
              {
                selected.Add(file.FullPath);
              }
            }
          }
          else if (view == ViewMode.Pinned)
          {
            if (pinnedList.Count > 0)
            {
              var file = pinnedList[pinnedCursor];
              pinned.Remove(file.FullPath);
              selected.Remove(file.FullPath);
            }
          }
          else
          {
            if (selectedList.Count > 0)
            {
              var file = selectedList[selectedCursor];
              pinned.Remove(file.FullPath);
              selected.Remove(file.FullPath);
            }
          }

          break;

        case ConsoleKey.U when key.Modifiers.HasFlag(ConsoleModifiers.Control):
          if (view == ViewMode.Search)
          {
            search = string.Empty;
            searchCursor = 0;
            searchScroll = 0;
          }

          break;

        case ConsoleKey.Backspace:
          if (view == ViewMode.Search)
          {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
              search = string.Empty;
              searchCursor = 0;
              searchScroll = 0;
            }
            else if (search.Length > 0)
            {
              search = search[..^1];
              searchCursor = 0;
              searchScroll = 0;
            }
          }

          break;

        case ConsoleKey.P when key.Modifiers.HasFlag(ConsoleModifiers.Control):
          if (view == ViewMode.Search)
          {
            if (filtered.Count > 0)
            {
              var file = filtered[searchCursor];

              if (pinned.Remove(file.FullPath))
              {
                selected.Remove(file.FullPath);
              }
              else
              {
                pinned.Add(file.FullPath);
                selected.Add(file.FullPath);
              }
            }
          }
          else if (view == ViewMode.Pinned)
          {
            if (pinnedList.Count > 0)
            {
              var file = pinnedList[pinnedCursor];
              pinned.Remove(file.FullPath);
              selected.Remove(file.FullPath);
            }
          }

          break;

        default:
          if (view == ViewMode.Search && !char.IsControl(key.KeyChar))
          {
            search += key.KeyChar;
            searchCursor = 0;
            searchScroll = 0;
          }

          break;
      }
    }
  }

  private static IReadOnlyList<CsFile> Filter(IReadOnlyList<CsFile> files, string search)
  {
    if (string.IsNullOrEmpty(search))
    {
      return files;
    }

    return files
      .Where(f => f.RelativePath.Contains(search, StringComparison.OrdinalIgnoreCase))
      .ToList();
  }

  private static List<CsFile> GetPinnedList(IReadOnlyList<CsFile> files, HashSet<string> pinned)
  {
    return files
      .Where(f => pinned.Contains(f.FullPath))
      .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  private static List<CsFile> GetSelectedList(IReadOnlyList<CsFile> files, HashSet<string> selected)
  {
    return files
      .Where(f => selected.Contains(f.FullPath))
      .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  private static int RenderSearch(
    string search,
    IReadOnlyList<CsFile> filtered,
    HashSet<string> selected,
    HashSet<string> pinned,
    int cursorIndex,
    int scrollOffset,
    string tokenEstimate)
  {
    var searchDisplay = string.IsNullOrEmpty(search) ? "[dim]<type to filter>[/]" : $"[yellow]{search}[/]";
    var matchCount = filtered.Count == 1 ? "1 match" : $"{filtered.Count} matches";

    AnsiConsole.MarkupLine($"[bold underline]Search[/]  [dim]Pinned ({pinned.Count})[/]  [dim]Selected ({selected.Count})[/]  [dim](Tab · Space select · Ctrl+P pin · Ctrl+U clear · Enter confirm)[/]");
    AnsiConsole.MarkupLine($"  Filter: {searchDisplay}  [dim]({matchCount} · {tokenEstimate})[/]");
    AnsiConsole.WriteLine();

    var visibleEnd = Math.Min(scrollOffset + MaxVisible, filtered.Count);

    for (var i = scrollOffset; i < visibleEnd; i++)
    {
      var file = filtered[i];
      var isSelected = selected.Contains(file.FullPath);
      var isPinned = pinned.Contains(file.FullPath);
      var isCursor = i == cursorIndex;

      var checkbox = isSelected ? "[green]+[/]" : "[dim]-[/]";
      var arrow = isCursor ? "[blue]>[/]" : " ";
      var pin = isPinned ? " [cyan]*[/]" : string.Empty;

      if (isCursor)
      {
        AnsiConsole.MarkupLine($" {arrow} {checkbox} [bold]{Markup.Escape(file.RelativePath)}[/]{pin}");
      }
      else
      {
        AnsiConsole.MarkupLine($" {arrow} {checkbox} [dim]{Markup.Escape(file.RelativePath)}[/]{pin}");
      }
    }

    var extraLines = 0;

    if (filtered.Count > MaxVisible)
    {
      var remaining = filtered.Count - visibleEnd;

      if (remaining > 0)
      {
        AnsiConsole.MarkupLine($"  [dim]... {remaining} more (scroll down)[/]");
        extraLines++;
      }
    }

    if (filtered.Count == 0)
    {
      AnsiConsole.MarkupLine("  [dim]No files match.[/]");
      extraLines++;
    }

    return 4 + (visibleEnd - scrollOffset) + extraLines;
  }

  private static int RenderPinned(
    List<CsFile> pinnedList,
    HashSet<string> selected,
    HashSet<string> pinned,
    int cursorIndex,
    int scrollOffset,
    string tokenEstimate)
  {
    AnsiConsole.MarkupLine($"[dim]Search[/]  [bold underline]Pinned ({pinned.Count})[/]  [dim]Selected ({selected.Count})[/]  {tokenEstimate}  [dim](Tab · Space/Ctrl+P unpin · Enter confirm)[/]");
    AnsiConsole.WriteLine();

    if (pinnedList.Count == 0)
    {
      AnsiConsole.MarkupLine("  [dim]No pinned files. Press Ctrl+P on a file in Search to pin it.[/]");

      return 4;
    }

    var visibleEnd = Math.Min(scrollOffset + MaxVisible, pinnedList.Count);

    for (var i = scrollOffset; i < visibleEnd; i++)
    {
      var file = pinnedList[i];
      var isSelected = selected.Contains(file.FullPath);
      var isCursor = i == cursorIndex;

      var checkbox = isSelected ? "[green]+[/]" : "[dim]-[/]";
      var arrow = isCursor ? "[blue]>[/]" : " ";

      if (isCursor)
      {
        AnsiConsole.MarkupLine($" {arrow} {checkbox} [cyan]*[/] [bold]{Markup.Escape(file.RelativePath)}[/]");
      }
      else
      {
        AnsiConsole.MarkupLine($" {arrow} {checkbox} [cyan]*[/] [dim]{Markup.Escape(file.RelativePath)}[/]");
      }
    }

    var extraLines = 0;

    if (pinnedList.Count > MaxVisible)
    {
      var remaining = pinnedList.Count - visibleEnd;

      if (remaining > 0)
      {
        AnsiConsole.MarkupLine($"  [dim]... {remaining} more (scroll down)[/]");
        extraLines++;
      }
    }

    return 3 + (visibleEnd - scrollOffset) + extraLines;
  }

  private static int RenderSelected(
    List<CsFile> selectedList,
    HashSet<string> selected,
    HashSet<string> pinned,
    int cursorIndex,
    int scrollOffset,
    string tokenEstimate)
  {
    AnsiConsole.MarkupLine($"[dim]Search[/]  [dim]Pinned ({pinned.Count})[/]  [bold underline]Selected ({selected.Count})[/]  {tokenEstimate}  [dim](Tab · Space deselect · Enter confirm)[/]");
    AnsiConsole.WriteLine();

    if (selectedList.Count == 0)
    {
      AnsiConsole.MarkupLine("  [dim]No files selected. Press Space on a file in Search to select it.[/]");

      return 4;
    }

    var visibleEnd = Math.Min(scrollOffset + MaxVisible, selectedList.Count);

    for (var i = scrollOffset; i < visibleEnd; i++)
    {
      var file = selectedList[i];
      var isPinned = pinned.Contains(file.FullPath);
      var isCursor = i == cursorIndex;

      var arrow = isCursor ? "[blue]>[/]" : " ";
      var pin = isPinned ? " [cyan]*[/]" : string.Empty;

      if (isCursor)
      {
        AnsiConsole.MarkupLine($" {arrow} [green]+[/] [bold]{Markup.Escape(file.RelativePath)}[/]{pin}");
      }
      else
      {
        AnsiConsole.MarkupLine($" {arrow} [green]+[/] [dim]{Markup.Escape(file.RelativePath)}[/]{pin}");
      }
    }

    var extraLines = 0;

    if (selectedList.Count > MaxVisible)
    {
      var remaining = selectedList.Count - visibleEnd;

      if (remaining > 0)
      {
        AnsiConsole.MarkupLine($"  [dim]... {remaining} more (scroll down)[/]");
        extraLines++;
      }
    }

    return 3 + (visibleEnd - scrollOffset) + extraLines;
  }

  private static string FormatTokenEstimate(IReadOnlyList<CsFile> files, HashSet<string> selected)
  {
    var totalBytes = files
      .Where(f => selected.Contains(f.FullPath))
      .Sum(f => f.SizeInBytes);

    var tokens = totalBytes / 4;

    return tokens switch
    {
      > 128_000 => $"[red]~{tokens:N0} tokens ⚠ over limit[/]",
      > 100_000 => $"[yellow]~{tokens:N0} tokens[/]",
      _ => $"[dim]~{tokens:N0} tokens[/]",
    };
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
