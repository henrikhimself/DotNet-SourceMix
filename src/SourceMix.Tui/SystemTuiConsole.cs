using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal sealed class SystemTuiConsole : ITuiConsole
{
  private const int FallbackWidth = 80;
  private const int FallbackHeight = 24;

  public IAnsiConsole Ansi => AnsiConsole.Console;

  public int WindowHeight
  {
    get
    {
      try
      {
        var height = Console.WindowHeight;

        return height > 0 ? height : FallbackHeight;
      }
      catch (IOException)
      {
        return FallbackHeight;
      }
    }
  }

  public int WindowWidth
  {
    get
    {
      try
      {
        var width = Console.WindowWidth;

        return width > 0 ? width : FallbackWidth;
      }
      catch (IOException)
      {
        return FallbackWidth;
      }
    }
  }

  public bool IsInteractive => !Console.IsInputRedirected && !Console.IsOutputRedirected;

  public void Write(string text)
  {
    Console.Write(text);
  }

  public void ClearLines(int lineCount)
  {
    if (lineCount <= 0)
    {
      return;
    }

    Console.Write($"\x1b[{lineCount}A\x1b[0J");
  }

  public void ClearScreen()
  {
    Console.Write("\x1b[2J\x1b[H");
  }
}
