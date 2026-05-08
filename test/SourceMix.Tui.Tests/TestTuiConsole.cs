using Spectre.Console;
using Spectre.Console.Testing;

namespace Hj.SourceMix.Tui.Tests;

internal sealed class TestTuiConsole : ITuiConsole, IDisposable
{
  private readonly TestConsole _ansi;

  public TestTuiConsole(int height = 24, int width = 100)
  {
    _ansi = new TestConsole();
    _ansi.Profile.Width = width;
    _ansi.Profile.Height = height;
    WindowHeight = height;
    WindowWidth = width;
  }

  public IAnsiConsole Ansi => _ansi;

  public int WindowHeight { get; private set; }

  public int WindowWidth { get; }

  public bool IsInteractive => true;

  public int ClearScreenCallCount { get; private set; }

  public string Output => _ansi.Output;

  public void Write(string text) => _ansi.Write(text);

  public void ClearLines(int lineCount)
  {
  }

  public void ClearScreen() => ClearScreenCallCount++;

  public void SetWindowHeight(int height)
  {
    WindowHeight = height;
    _ansi.Profile.Height = height;
  }

  public void Dispose() => _ansi.Dispose();
}
