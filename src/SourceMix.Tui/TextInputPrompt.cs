using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal static class TextInputPrompt
{
  /// <summary>
  /// Reads a single-line text value with Ctrl+Q-to-back and Esc-to-quit support.
  /// </summary>
  /// <param name="label">Markup label printed before the input value.</param>
  /// <param name="initial">Initial editable text.</param>
  /// <param name="console">TUI console used for rendering.</param>
  /// <param name="keys">Key reader used for input.</param>
  /// <param name="validate">
  /// Optional validator. If it returns a non-null error, the input remains
  /// in edit mode and the error is displayed below the prompt until the next
  /// keystroke.
  /// </param>
  /// <returns>
  /// A tuple of the entered text, a flag indicating whether the user
  /// confirmed (Enter), and a flag indicating whether the user requested an
  /// immediate quit (Esc). Ctrl+Q yields <c>Confirmed=false, Quit=false</c>
  /// (the back/skip path).
  /// </returns>
  public static (string Text, bool Confirmed, bool Quit) Read(
    string label,
    string initial,
    ITuiConsole console,
    IKeyReader keys,
    Func<string, string?>? validate = null)
  {
    ArgumentNullException.ThrowIfNull(label);
    ArgumentNullException.ThrowIfNull(initial);
    ArgumentNullException.ThrowIfNull(console);
    ArgumentNullException.ThrowIfNull(keys);

    var text = initial;
    string? error = null;
    var pollInterval = TimeSpan.FromMilliseconds(150);

    while (true)
    {
      var rendered = Render(console, label, text, error);
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
        console.ClearScreen();
        continue;
      }

      switch (key.Key)
      {
        case ConsoleKey.Enter:
          if (validate is not null)
          {
            error = validate(text);

            if (error is not null)
            {
              continue;
            }
          }

          console.Ansi.MarkupLine($"{label} {Markup.Escape(text)}");
          return (text, true, false);

        case ConsoleKey.Escape:
          console.Ansi.WriteLine();
          return (text, false, true);

        case ConsoleKey.Q when (key.Modifiers & ConsoleModifiers.Control) != 0:
          return (text, false, false);

        case ConsoleKey.Backspace:
          if (text.Length > 0)
          {
            text = text[..^1];
            error = null;
          }

          break;

        default:
          if (key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
          {
            text += key.KeyChar;
            error = null;
          }

          break;
      }
    }
  }

  private static int Render(ITuiConsole console, string label, string text, string? error)
  {
    var ansi = console.Ansi;
    var width = console.WindowWidth;
    var line = $"{label} {Markup.Escape(text)}\u2588";
    ansi.MarkupLine(line);
    var lines = TuiRender.RowsFor(line, width);

    const string HintLine = "  [dim](Enter confirm \u00b7 Ctrl+Q back)[/]";
    ansi.MarkupLine(HintLine);
    lines += TuiRender.RowsFor(HintLine, width);

    if (error is not null)
    {
      var errorLine = $"  [red]{Markup.Escape(error)}[/]";
      ansi.MarkupLine(errorLine);
      lines += TuiRender.RowsFor(errorLine, width);
    }

    return lines;
  }
}
