using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal sealed class ListPickerPrompt<T>
  where T : notnull
{
  public required string Header { get; init; }

  public required IReadOnlyList<T> Items { get; init; }

  public required Func<T, string> KeySelector { get; init; }

  public required Func<T, ListPickerItemState, string> Renderer { get; init; }

  public Func<string, IReadOnlyList<T>>? Filter { get; init; }

  public bool MultiSelect { get; init; }

  public bool AllowPin { get; init; }

  /// <summary>
  /// Gets a value indicating whether the picker enforces a pin↔selection
  /// linkage: pinning a row also selects it, unpinning a row also deselects
  /// it, deselecting a row also unpins it, and Ctrl+U clears both sets.
  /// </summary>
  public bool LinkPinAndSelection { get; init; }

  public IReadOnlySet<string>? InitialSelected { get; init; }

  public IReadOnlySet<string>? InitialPinned { get; init; }

  /// <summary>
  /// Gets a key for the item the cursor should be placed on when the picker
  /// first renders. Falls back to index 0 if no item with this key is found.
  /// </summary>
  public string? InitialCursorKey { get; init; }

  public Func<ConsoleKeyInfo, ListPickerKeyAction>? KeyOverride { get; init; }

  /// <summary>
  /// Gets an optional callback invoked when the user presses Ctrl+D on the
  /// current item. The callback may mutate external state (for example to
  /// remove a custom prompt from preferences). When <see cref="ListPickerDeleteResult.Removed"/>
  /// is returned, the picker rebuilds the visible list using <see cref="ItemsSource"/>.
  /// </summary>
  public Func<T, ITuiConsole, IKeyReader, ListPickerDeleteResult>? DeleteHandler { get; init; }

  /// <summary>
  /// Gets an optional callback that returns a refreshed snapshot of the
  /// items. Used after <see cref="DeleteHandler"/> reports a removal so the
  /// picker can rebuild the visible list.
  /// </summary>
  public Func<IReadOnlyList<T>>? ItemsSource { get; init; }

  public ListPickerResult<T> Show(ITuiConsole console, IKeyReader keys)
  {
    ArgumentNullException.ThrowIfNull(console);
    ArgumentNullException.ThrowIfNull(keys);

    var comparer = StringComparer.OrdinalIgnoreCase;
    var selected = new HashSet<string>(InitialSelected ?? (IReadOnlySet<string>)new HashSet<string>(comparer), comparer);
    var pinned = new HashSet<string>(InitialPinned ?? (IReadOnlySet<string>)new HashSet<string>(comparer), comparer);

    var filterText = string.Empty;
    var current = Filter is not null ? Filter(filterText) : Items;
    var cursor = 0;
    var scroll = 0;
    var pollInterval = TimeSpan.FromMilliseconds(150);
    var items = Items;

    if (InitialCursorKey is not null && current.Count > 0)
    {
      for (var i = 0; i < current.Count; i++)
      {
        if (string.Equals(KeySelector(current[i]), InitialCursorKey, StringComparison.OrdinalIgnoreCase))
        {
          cursor = i;
          break;
        }
      }
    }

    while (true)
    {
      if (current.Count == 0)
      {
        cursor = 0;
        scroll = 0;
      }
      else
      {
        if (cursor < 0)
        {
          cursor = 0;
        }

        if (cursor >= current.Count)
        {
          cursor = current.Count - 1;
        }
      }

      const int ReservedRows = 10;
      var maxVisible = Math.Max(5, console.WindowHeight - ReservedRows);

      if (current.Count > 0)
      {
        if (scroll > cursor)
        {
          scroll = cursor;
        }

        if (cursor >= scroll + maxVisible)
        {
          scroll = cursor - maxVisible + 1;
        }

        if (scroll < 0)
        {
          scroll = 0;
        }
      }
      else
      {
        scroll = 0;
      }

      var rendered = Render(console, current, filterText, cursor, scroll, maxVisible, selected, pinned);
      var lastWindowHeight = console.WindowHeight;

      ConsoleKeyInfo key;
      bool gotKey;

      while (true)
      {
        gotKey = keys.TryReadKey(pollInterval, out key);

        if (gotKey)
        {
          break;
        }

        if (console.WindowHeight != lastWindowHeight)
        {
          break;
        }
      }

      console.ClearLines(rendered);

      if (!gotKey)
      {
        // Window resize detected. Wipe the screen so a redraw at a different
        // height can't leave residue from the previous frame's wrap rows.
        TuiRender.ResetScreenWithAppHeader(console);
        continue;
      }

      var ctrl = (key.Modifiers & ConsoleModifiers.Control) != 0;

      if (KeyOverride is not null)
      {
        var action = KeyOverride(key);

        if (action == ListPickerKeyAction.Exit)
        {
          console.Ansi.WriteLine();

          return BuildResult(current, cursor, selected, pinned, ListPickerExitReason.KeyOverride, key);
        }

        if (action == ListPickerKeyAction.Handled)
        {
          continue;
        }
      }

      switch (key.Key)
      {
        case ConsoleKey.Enter:
          console.Ansi.WriteLine();
          return BuildResult(current, cursor, selected, pinned, ListPickerExitReason.Confirmed);

        case ConsoleKey.Escape:
          console.Ansi.WriteLine();
          return new ListPickerResult<T>([], pinned, ListPickerExitReason.Quit);

        case ConsoleKey.Q when ctrl:
          return new ListPickerResult<T>([], pinned, ListPickerExitReason.Skipped);

        case ConsoleKey.UpArrow:
          if (cursor > 0)
          {
            cursor--;
          }

          break;

        case ConsoleKey.DownArrow:
          if (cursor < current.Count - 1)
          {
            cursor++;
          }

          break;

        case ConsoleKey.PageUp:
          cursor = Math.Max(0, cursor - maxVisible);
          break;

        case ConsoleKey.PageDown:
          cursor = Math.Min(Math.Max(0, current.Count - 1), cursor + maxVisible);
          break;

        case ConsoleKey.Home:
          cursor = 0;
          break;

        case ConsoleKey.End:
          cursor = Math.Max(0, current.Count - 1);
          break;

        case ConsoleKey.Spacebar when MultiSelect:
          if (current.Count > 0)
          {
            var k = KeySelector(current[cursor]);

            if (selected.Remove(k))
            {
              if (LinkPinAndSelection)
              {
                pinned.Remove(k);
              }
            }
            else
            {
              selected.Add(k);
            }
          }

          break;

        case ConsoleKey.P when ctrl && AllowPin:
          if (current.Count > 0)
          {
            var k = KeySelector(current[cursor]);

            if (pinned.Remove(k))
            {
              if (LinkPinAndSelection)
              {
                selected.Remove(k);
              }
            }
            else
            {
              pinned.Add(k);

              if (LinkPinAndSelection)
              {
                selected.Add(k);
              }
            }
          }

          break;

        case ConsoleKey.U when ctrl && Filter is not null:
          if (filterText.Length > 0)
          {
            filterText = string.Empty;
            current = Filter(filterText);
            cursor = 0;
            scroll = 0;
          }

          break;

        case ConsoleKey.D when ctrl && DeleteHandler is not null:
          if (current.Count > 0)
          {
            var target = current[cursor];
            var deleteResult = DeleteHandler(target, console, keys);

            if (deleteResult == ListPickerDeleteResult.Removed)
            {
              if (ItemsSource is not null)
              {
                items = ItemsSource();
              }
              else
              {
                items = [.. items.Where(i => !ReferenceEquals(i, target) && !string.Equals(KeySelector(i), KeySelector(target), StringComparison.OrdinalIgnoreCase))];
              }

              current = Filter is not null ? Filter(filterText) : items;

              if (cursor >= current.Count)
              {
                cursor = Math.Max(0, current.Count - 1);
              }

              scroll = 0;
            }

            TuiRender.ResetScreenWithAppHeader(console);
          }

          break;

        case ConsoleKey.Backspace when Filter is not null:
          if (filterText.Length > 0)
          {
            filterText = filterText[..^1];
            current = Filter(filterText);
            cursor = 0;
            scroll = 0;
          }

          break;

        default:
          if (Filter is not null && !ctrl && key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
          {
            filterText += key.KeyChar;
            current = Filter(filterText);
            cursor = 0;
            scroll = 0;
          }

          break;
      }
    }
  }

  private ListPickerResult<T> BuildResult(
    IReadOnlyList<T> current,
    int cursor,
    HashSet<string> selected,
    HashSet<string> pinned,
    ListPickerExitReason reason,
    ConsoleKeyInfo? overrideKey = null)
  {
    List<T> selectedItems;

    if (MultiSelect)
    {
      selectedItems = [.. Items.Where(item => selected.Contains(KeySelector(item)))];
    }
    else if (current.Count > 0)
    {
      selectedItems = [current[cursor]];
    }
    else
    {
      selectedItems = [];
    }

    return new ListPickerResult<T>(selectedItems, pinned, reason, overrideKey)
    {
      SelectedKeys = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase),
    };
  }

  private int Render(
    ITuiConsole console,
    IReadOnlyList<T> items,
    string filterText,
    int cursor,
    int scroll,
    int maxVisible,
    HashSet<string> selected,
    HashSet<string> pinned)
  {
    var ansi = console.Ansi;
    var width = console.WindowWidth;
    ansi.MarkupLine(Header);
    var lines = TuiRender.RowsFor(Header, width);

    if (Filter is not null)
    {
      var filterLine = $"[dim]Filter:[/] {Markup.Escape(filterText)}";
      ansi.MarkupLine(filterLine);
      lines += TuiRender.RowsFor(filterLine, width);
    }

    if (items.Count == 0)
    {
      const string Empty = "  [dim]No items.[/]";
      ansi.MarkupLine(Empty);
      lines += TuiRender.RowsFor(Empty, width);

      return lines;
    }

    var hasAbove = scroll > 0;

    if (hasAbove)
    {
      var aboveLine = $"  [dim]... {scroll} more above[/]";
      ansi.MarkupLine(aboveLine);
      lines += TuiRender.RowsFor(aboveLine, width);
    }

    var visibleCount = Math.Min(maxVisible, items.Count - scroll);

    for (var i = scroll; i < scroll + visibleCount; i++)
    {
      var key = KeySelector(items[i]);
      var state = new ListPickerItemState(
        IsCursor: i == cursor,
        IsSelected: selected.Contains(key),
        IsPinned: pinned.Contains(key));
      var row = Renderer(items[i], state);
      ansi.MarkupLine(row);
      lines += TuiRender.RowsFor(row, width);
    }

    var below = items.Count - (scroll + visibleCount);

    if (below > 0)
    {
      var belowLine = $"  [dim]... {below} more below[/]";
      ansi.MarkupLine(belowLine);
      lines += TuiRender.RowsFor(belowLine, width);
    }

    return lines;
  }
}
