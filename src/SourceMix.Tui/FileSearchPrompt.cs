using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal static class FileSearchPrompt
{
  private enum ViewMode
  {
    Search,
    Pinned,
    Selected,
  }

  internal static FileSelection Show(
    IReadOnlyList<CsFile> files,
    IReadOnlySet<string> pinnedPaths,
    ITuiConsole? tuiConsole = null,
    IKeyReader? keys = null)
  {
    ArgumentNullException.ThrowIfNull(files);
    ArgumentNullException.ThrowIfNull(pinnedPaths);

    tuiConsole ??= new SystemTuiConsole();
    keys ??= new ConsoleKeyReader();

    var comparer = StringComparer.OrdinalIgnoreCase;
    var selected = new HashSet<string>(pinnedPaths, comparer);
    var pinned = new HashSet<string>(pinnedPaths, comparer);

    var view = ViewMode.Search;

    while (true)
    {
      var result = view switch
      {
        ViewMode.Search => RunSearch(files, selected, pinned, tuiConsole, keys),
        ViewMode.Pinned => RunPinned(files, selected, pinned, tuiConsole, keys),
        _ => RunSelected(files, selected, pinned, tuiConsole, keys),
      };

      SyncState(result, selected, pinned);

      switch (result.Reason)
      {
        case ListPickerExitReason.Confirmed:
          return new FileSelection(SelectedInOriginalOrder(files, selected), pinned);

        case ListPickerExitReason.Skipped:
          return new FileSelection([], new HashSet<string>(comparer)) { Step = StepResult.Back };

        case ListPickerExitReason.Quit:
          return new FileSelection([], new HashSet<string>(comparer)) { Step = StepResult.Quit };

        case ListPickerExitReason.KeyOverride when result.OverrideKey?.Key == ConsoleKey.Tab:
          view = view switch
          {
            ViewMode.Search => ViewMode.Pinned,
            ViewMode.Pinned => ViewMode.Selected,
            _ => ViewMode.Search,
          };
          tuiConsole.ClearScreen();
          TuiRender.WriteAppHeading(tuiConsole);
          break;

        default:
          return new FileSelection([], new HashSet<string>(comparer));
      }
    }
  }

  private static ListPickerResult<CsFile> RunSearch(
    IReadOnlyList<CsFile> files,
    HashSet<string> selected,
    HashSet<string> pinned,
    ITuiConsole console,
    IKeyReader keys)
  {
    var picker = new ListPickerPrompt<CsFile>
    {
      Header = "[bold underline]Search[/]  [dim]Pinned (P)[/]  [dim]Selected (S)[/]  [dim](Tab \u00b7 Space select/deselect \u00b7 Ctrl+P pin/unpin \u00b7 Ctrl+U clear search \u00b7 Enter confirm \u00b7 Ctrl+Q quit)[/]",
      Items = files,
      KeySelector = static f => f.FullPath,
      MultiSelect = true,
      AllowPin = true,
      LinkPinAndSelection = true,
      InitialSelected = selected,
      InitialPinned = pinned,
      Filter = text => Filter(files, text),
      Renderer = RenderSearchRow,
      KeyOverride = TabOverride,
    };

    return picker.Show(console, keys);
  }

  private static ListPickerResult<CsFile> RunPinned(
    IReadOnlyList<CsFile> files,
    HashSet<string> selected,
    HashSet<string> pinned,
    ITuiConsole console,
    IKeyReader keys)
  {
    var pinnedList = files
      .Where(f => pinned.Contains(f.FullPath))
      .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
      .ToList();

    var picker = new ListPickerPrompt<CsFile>
    {
      Header = "[dim]Search[/]  [bold underline]Pinned (P)[/]  [dim]Selected (S)[/]  [dim](Tab \u00b7 Ctrl+P unpin/pin \u00b7 Enter confirm \u00b7 Ctrl+Q quit)[/]",
      Items = pinnedList,
      KeySelector = static f => f.FullPath,
      MultiSelect = true,
      AllowPin = true,
      LinkPinAndSelection = false,
      InitialSelected = selected,
      InitialPinned = pinned,
      Renderer = RenderPinnedRow,
      KeyOverride = TabOverride,
    };

    return picker.Show(console, keys);
  }

  private static ListPickerResult<CsFile> RunSelected(
    IReadOnlyList<CsFile> files,
    HashSet<string> selected,
    HashSet<string> pinned,
    ITuiConsole console,
    IKeyReader keys)
  {
    var selectedList = files
      .Where(f => selected.Contains(f.FullPath))
      .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
      .ToList();

    var picker = new ListPickerPrompt<CsFile>
    {
      Header = "[dim]Search[/]  [dim]Pinned (P)[/]  [bold underline]Selected (S)[/]  [dim](Tab \u00b7 Space deselect/select \u00b7 Ctrl+P pin/unpin \u00b7 Enter confirm \u00b7 Ctrl+Q quit)[/]",
      Items = selectedList,
      KeySelector = static f => f.FullPath,
      MultiSelect = true,
      AllowPin = true,
      LinkPinAndSelection = false,
      InitialSelected = selected,
      InitialPinned = pinned,
      Renderer = RenderSelectedRow,
      KeyOverride = TabOverride,
    };

    return picker.Show(console, keys);
  }

  private static ListPickerKeyAction TabOverride(ConsoleKeyInfo key)
    => key.Key == ConsoleKey.Tab ? ListPickerKeyAction.Exit : ListPickerKeyAction.NotHandled;

  private static IReadOnlyList<CsFile> Filter(IReadOnlyList<CsFile> files, string search)
  {
    if (string.IsNullOrEmpty(search))
    {
      return files;
    }

    var result = new List<CsFile>();

    foreach (var file in files)
    {
      if (file.RelativePath.Contains(search, StringComparison.OrdinalIgnoreCase))
      {
        result.Add(file);
      }
    }

    return result;
  }

  private static string RenderSearchRow(CsFile file, ListPickerItemState state)
  {
    var arrow = state.IsCursor ? "[darkorange]>[/]" : " ";
    var checkbox = state.IsSelected ? "[green]+[/]" : "[dim]-[/]";
    var pin = state.IsPinned ? " [cyan]*[/]" : string.Empty;
    var label = state.IsCursor
      ? $"[bold]{Markup.Escape(file.RelativePath)}[/]"
      : $"[dim]{Markup.Escape(file.RelativePath)}[/]";

    return $" {arrow} {checkbox} {label}{pin}";
  }

  private static string RenderPinnedRow(CsFile file, ListPickerItemState state)
  {
    var arrow = state.IsCursor ? "[darkorange]>[/]" : " ";
    var checkbox = state.IsSelected ? "[green]+[/]" : "[dim]-[/]";
    var pin = state.IsPinned ? " [cyan]*[/]" : string.Empty;
    var label = state.IsCursor
      ? $"[bold]{Markup.Escape(file.RelativePath)}[/]"
      : $"[dim]{Markup.Escape(file.RelativePath)}[/]";

    return $" {arrow} {checkbox} {label}{pin}";
  }

  private static string RenderSelectedRow(CsFile file, ListPickerItemState state)
  {
    var arrow = state.IsCursor ? "[darkorange]>[/]" : " ";
    var checkbox = state.IsSelected ? "[green]+[/]" : "[dim]-[/]";
    var pin = state.IsPinned ? " [cyan]*[/]" : string.Empty;
    var label = state.IsCursor
      ? $"[bold]{Markup.Escape(file.RelativePath)}[/]"
      : $"[dim]{Markup.Escape(file.RelativePath)}[/]";

    return $" {arrow} {checkbox} {label}{pin}";
  }

  private static void SyncState(
    ListPickerResult<CsFile> result,
    HashSet<string> selected,
    HashSet<string> pinned)
  {
    selected.Clear();

    if (result.SelectedKeys is { } keys)
    {
      foreach (var key in keys)
      {
        selected.Add(key);
      }
    }
    else
    {
      foreach (var item in result.Selected)
      {
        selected.Add(item.FullPath);
      }
    }

    pinned.Clear();

    foreach (var path in result.Pinned)
    {
      pinned.Add(path);
    }
  }

  private static List<string> SelectedInOriginalOrder(IReadOnlyList<CsFile> files, HashSet<string> selected)
  {
    var ordered = new List<string>(selected.Count);

    foreach (var file in files)
    {
      if (selected.Contains(file.FullPath))
      {
        ordered.Add(file.FullPath);
      }
    }

    return ordered;
  }
}
