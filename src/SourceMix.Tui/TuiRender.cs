using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal static class TuiRender
{
  /// <summary>
  /// Writes the application heading (yellow "SourceMix") followed by a blank
  /// line. Called at the top of each wizard step so every step renders the
  /// same brand row.
  /// </summary>
  /// <param name="console">The TUI console to write the heading to.</param>
  public static void WriteAppHeading(ITuiConsole console)
  {
    ArgumentNullException.ThrowIfNull(console);

    console.Ansi.MarkupLine("[yellow]SourceMix.[/]");
    console.Ansi.WriteLine();
  }

  /// <summary>
  /// Clears the full screen and immediately restores the shared app heading.
  /// Use this for redraw paths that wipe the terminal canvas mid-step.
  /// </summary>
  /// <param name="console">The TUI console to clear and redraw.</param>
  public static void ResetScreenWithAppHeader(ITuiConsole console)
  {
    ArgumentNullException.ThrowIfNull(console);

    console.ClearScreen();
    WriteAppHeading(console);
  }

  /// <summary>
  /// Returns the number of terminal rows occupied by a single rendered markup
  /// line, accounting for line-wrapping when the plain text exceeds the
  /// console window width.
  /// </summary>
  /// <param name="markup">The rendered markup string (Spectre.Console markup tags allowed).</param>
  /// <param name="windowWidth">The current console window width in columns.</param>
  /// <returns>The number of terminal rows the line occupies (always at least 1).</returns>
  public static int RowsFor(string markup, int windowWidth)
  {
    if (windowWidth <= 0)
    {
      return 1;
    }

    var plain = Markup.Remove(markup);

    if (string.IsNullOrEmpty(plain))
    {
      return 1;
    }

    return Math.Max(1, (plain.Length + windowWidth - 1) / windowWidth);
  }
}
