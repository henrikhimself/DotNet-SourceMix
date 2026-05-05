using System.IO.Abstractions.TestingHelpers;

namespace Hj.SourceMix.Tui.Tests;

public sealed class OutputPathPromptTests
{
  [Fact]
  public void Enter_AcceptsInitialPath()
  {
    using var console = new TestTuiConsole();
    var fs = MakeFs();
    var keys = new FakeKeyReader([K(ConsoleKey.Enter)]);

    var result = OutputPathPrompt.Show("/repo/out.md", console, keys, fs);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.Equal("/repo/out.md", result.Path);
  }

  [Fact]
  public void CtrlQ_ReturnsBack()
  {
    using var console = new TestTuiConsole();
    var fs = MakeFs();
    var keys = new FakeKeyReader([CtrlQ()]);

    var result = OutputPathPrompt.Show("/repo/out.md", console, keys, fs);

    Assert.Equal(StepResult.Back, result.Step);
    Assert.Null(result.Path);
  }

  [Fact]
  public void Esc_ReturnsQuit()
  {
    using var console = new TestTuiConsole();
    var fs = MakeFs();
    var keys = new FakeKeyReader([K(ConsoleKey.Escape)]);

    var result = OutputPathPrompt.Show("/repo/out.md", console, keys, fs);

    Assert.Equal(StepResult.Quit, result.Step);
    Assert.Null(result.Path);
  }

  [Fact]
  public void AcceptsCustomPath_WhenDirectoryExists()
  {
    using var console = new TestTuiConsole();
    var fs = MakeFs();
    fs.Directory.CreateDirectory("/repo/sub");

    var initial = "/repo/out.md";
    var keysList = new List<ConsoleKeyInfo>();

    for (var i = 0; i < initial.Length; i++)
    {
      keysList.Add(K(ConsoleKey.Backspace, '\b'));
    }

    foreach (var c in "/repo/sub/x.md")
    {
      keysList.Add(Char(c));
    }

    keysList.Add(K(ConsoleKey.Enter));

    var keys = new FakeKeyReader(keysList);

    var result = OutputPathPrompt.Show(initial, console, keys, fs);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.Equal("/repo/sub/x.md", result.Path);
  }

  private static MockFileSystem MakeFs()
  {
    var fs = new MockFileSystem();
    fs.Directory.CreateDirectory("/repo");

    return fs;
  }

  private static ConsoleKeyInfo K(ConsoleKey key, char ch = '\0')
    => new(ch, key, shift: false, alt: false, control: false);

  private static ConsoleKeyInfo CtrlQ()
    => new('\x11', ConsoleKey.Q, shift: false, alt: false, control: true);

  private static ConsoleKeyInfo Char(char c)
  {
    var key = char.IsLetter(c)
      ? (ConsoleKey)char.ToUpperInvariant(c)
      : char.IsDigit(c)
        ? (ConsoleKey)c
        : ConsoleKey.Oem1;

    return new ConsoleKeyInfo(c, key, shift: false, alt: false, control: false);
  }
}
