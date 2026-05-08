namespace Hj.SourceMix.Tui.Tests;

public sealed class TextInputPromptTests
{
  [Fact]
  public void Read_TypeAndConfirm_ReturnsTextWithConfirmed()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      Char('a'),
      Char('b'),
      Char('c'),
      K(ConsoleKey.Enter),
    ]);

    var (text, confirmed, quit) = TextInputPrompt.Read("X:", string.Empty, console, keys);

    Assert.Equal("abc", text);
    Assert.True(confirmed);
    Assert.False(quit);
  }

  [Fact]
  public void Read_Backspace_TrimsLastChar()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      Char('a'),
      Char('b'),
      K(ConsoleKey.Backspace),
      Char('c'),
      K(ConsoleKey.Enter),
    ]);

    var (text, confirmed, _) = TextInputPrompt.Read("X:", string.Empty, console, keys);

    Assert.Equal("ac", text);
    Assert.True(confirmed);
  }

  [Fact]
  public void Read_Esc_ReturnsQuitTrueConfirmedFalse()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      Char('a'),
      K(ConsoleKey.Escape),
    ]);

    var (_, confirmed, quit) = TextInputPrompt.Read("X:", string.Empty, console, keys);

    Assert.False(confirmed);
    Assert.True(quit);
  }

  [Fact]
  public void Read_CtrlQ_ReturnsBackConfirmedFalseQuitFalse()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      Char('a'),
      K(ConsoleKey.Q, 'q', ctrl: true),
    ]);

    var (_, confirmed, quit) = TextInputPrompt.Read("X:", string.Empty, console, keys);

    Assert.False(confirmed);
    Assert.False(quit);
  }

  [Fact]
  public void Read_InitialValuePresent_RetainedOnConfirm()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.Enter)]);

    var (text, confirmed, _) = TextInputPrompt.Read("X:", "seed", console, keys);

    Assert.Equal("seed", text);
    Assert.True(confirmed);
  }

  [Fact]
  public void Read_FailingValidator_BlocksConfirmUntilValid()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Enter),       // initial empty fails
      Char('x'),
      K(ConsoleKey.Enter),       // now passes
    ]);

    var (text, confirmed, _) = TextInputPrompt.Read(
      "X:",
      string.Empty,
      console,
      keys,
      static t => string.IsNullOrEmpty(t) ? "empty" : null);

    Assert.Equal("x", text);
    Assert.True(confirmed);
  }

  [Fact]
  public void Read_LongInputOverflowingWidth_RendersAndReturnsFullText()
  {
    using var console = new TestTuiConsole(width: 20);
    var keysList = new List<ConsoleKeyInfo>();
    var expected = new string('x', 60);

    for (var i = 0; i < expected.Length; i++)
    {
      keysList.Add(Char('x'));
    }

    keysList.Add(K(ConsoleKey.Enter));
    var keys = new FakeKeyReader(keysList);

    var (text, confirmed, _) = TextInputPrompt.Read("X:", string.Empty, console, keys);

    Assert.Equal(expected, text);
    Assert.True(confirmed);
  }

  [Fact]
  public void Read_ValidatorOnlyRunsOnEnter_NotPerKeystroke()
  {
    using var console = new TestTuiConsole();
    var calls = 0;

    var keys = new FakeKeyReader(
    [
      Char('a'),
      Char('b'),
      Char('c'),
      K(ConsoleKey.Backspace),
      K(ConsoleKey.Enter),
    ]);

    var (text, confirmed, _) = TextInputPrompt.Read(
      "X:",
      string.Empty,
      console,
      keys,
      t =>
      {
        calls++;

        return null;
      });

    Assert.Equal("ab", text);
    Assert.True(confirmed);
    Assert.Equal(1, calls);
  }

  [Fact]
  public void Read_ResizeDuringInput_RedrawsWithoutLosingTypedText()
  {
    using var console = new TestTuiConsole(height: 24);
    var keys = new FakeKeyReader([]);
    keys.Enqueue(Char('a'));
    keys.Enqueue(Char('b'));
    keys.EnqueueIdle();
    keys.EnqueueAction(() => console.SetWindowHeight(12));
    keys.Enqueue(Char('c'));
    keys.Enqueue(K(ConsoleKey.Enter));

    var (text, confirmed, _) = TextInputPrompt.Read("X:", string.Empty, console, keys);

    Assert.Equal("abc", text);
    Assert.True(confirmed);
    Assert.True(console.ClearScreenCallCount >= 1);
    Assert.Contains("SourceMix", console.Output, StringComparison.Ordinal);
  }

  [Fact]
  public void Read_RendersHintLine_WithCtrlQAndEsc()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.Enter)]);

    _ = TextInputPrompt.Read("X:", "seed", console, keys);

    Assert.Contains("Ctrl+Q back", console.Output, StringComparison.Ordinal);
  }

  private static ConsoleKeyInfo K(ConsoleKey key, char ch = '\0', bool ctrl = false)
    => new(ch, key, shift: false, alt: false, control: ctrl);

  private static ConsoleKeyInfo Char(char c)
  {
    var key = char.IsLetter(c)
      ? (ConsoleKey)char.ToUpperInvariant(c)
      : ConsoleKey.Oem1;

    return new ConsoleKeyInfo(c, key, shift: false, alt: false, control: false);
  }
}
