using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal sealed record FileSelection(
  IReadOnlyList<string> SelectedPaths,
  IReadOnlySet<string> PinnedPaths);

internal static class FileSearchPrompt
{
  private const int MaxVisible = 20;

  private enum ViewMode { Search, Pinned }

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

      int renderedLines;

      if (view == ViewMode.Search)
      {
        renderedLines = RenderSearch(search, filtered, selected, pinned, searchCursor, searchScroll);
      }
      else
      {
        renderedLines = RenderPinned(pinnedList, selected, pinned, pinnedCursor, pinnedScroll);
      }

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
          view = view == ViewMode.Search ? ViewMode.Pinned : ViewMode.Search;
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
          else
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
          else
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
          else
          {
            if (pinnedList.Count > 0)
            {
              var file = pinnedList[pinnedCursor];
              pinned.Remove(file.FullPath);
              selected.Remove(file.FullPath);
            }
          }

          break;

        case ConsoleKey.Backspace:
          if (view == ViewMode.Search && search.Length > 0)
          {
            search = search[..^1];
            searchCursor = 0;
            searchScroll = 0;
          }

          break;

        default:
          if (key.KeyChar is 'p' or 'P')
          {
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
            else
            {
              if (pinnedList.Count > 0)
              {
                var file = pinnedList[pinnedCursor];
                pinned.Remove(file.FullPath);
                selected.Remove(file.FullPath);
              }
            }

            break;
          }

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

  private static int RenderSearch(
    string search,
    IReadOnlyList<CsFile> filtered,
    HashSet<string> selected,
    HashSet<string> pinned,
    int cursorIndex,
    int scrollOffset)
  {
    var searchDisplay = string.IsNullOrEmpty(search) ? "[dim]<type to filter>[/]" : $"[yellow]{search}[/]";
    var matchCount = filtered.Count == 1 ? "1 match" : $"{filtered.Count} matches";

    AnsiConsole.MarkupLine($"[bold underline]Search[/]  [dim]Pinned ({pinned.Count})[/]  [dim](Tab switch · Space select · P pin · Enter confirm)[/]");
    AnsiConsole.MarkupLine($"  Filter: {searchDisplay}  [dim]({matchCount} · {selected.Count} selected)[/]");
    AnsiConsole.WriteLine();

    var visibleEnd = Math.Min(scrollOffset + MaxVisible, filtered.Count);

    for (var i = scrollOffset; i < visibleEnd; i++)
    {
      var file = filtered[i];
      var isSelected = selected.Contains(file.FullPath);
      var isPinned = pinned.Contains(file.FullPath);
      var isCursor = i == cursorIndex;

      var checkbox = isSelected ? "[green]■[/]" : "[dim]□[/]";
      var arrow = isCursor ? "[blue]>[/]" : " ";
      var pin = isPinned ? " [cyan]●[/]" : string.Empty;

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
    int scrollOffset)
  {
    AnsiConsole.MarkupLine($"[dim]Search[/]  [bold underline]Pinned ({pinned.Count})[/]  [dim](Tab switch · Space/P unpin · Enter confirm)[/]");
    AnsiConsole.WriteLine();

    if (pinnedList.Count == 0)
    {
      AnsiConsole.MarkupLine("  [dim]No pinned files. Press P on a file in Search to pin it.[/]");

      return 4;
    }

    var visibleEnd = Math.Min(scrollOffset + MaxVisible, pinnedList.Count);

    for (var i = scrollOffset; i < visibleEnd; i++)
    {
      var file = pinnedList[i];
      var isSelected = selected.Contains(file.FullPath);
      var isCursor = i == cursorIndex;

      var checkbox = isSelected ? "[green]■[/]" : "[dim]□[/]";
      var arrow = isCursor ? "[blue]>[/]" : " ";

      if (isCursor)
      {
        AnsiConsole.MarkupLine($" {arrow} {checkbox} [cyan]●[/] [bold]{Markup.Escape(file.RelativePath)}[/]");
      }
      else
      {
        AnsiConsole.MarkupLine($" {arrow} {checkbox} [cyan]●[/] [dim]{Markup.Escape(file.RelativePath)}[/]");
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
