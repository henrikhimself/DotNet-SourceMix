using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal interface ITuiConsole
{
  IAnsiConsole Ansi { get; }

  int WindowHeight { get; }

  int WindowWidth { get; }

  bool IsInteractive { get; }

  void Write(string text);

  void ClearLines(int lineCount);

  void ClearScreen();
}
