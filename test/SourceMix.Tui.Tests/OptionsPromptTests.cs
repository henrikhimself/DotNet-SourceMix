using Hj.SourceMix.Core;

namespace Hj.SourceMix.Tui.Tests;

public sealed class OptionsPromptTests
{
  [Fact]
  public void Toggle_RecursiveSpace_FlipsBoolInResult()
  {
    using var console = new TestTuiConsole();
    var prefs = new SolutionPreferences();

    // Toggles step: Space (Recursive on), Enter.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
    ]);

    var result = OptionsPrompt.Show(prefs, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.NotNull(result.Values);
    Assert.True(result.Values!.Recursive);
    Assert.False(result.Values.IncludeCompiled);
    Assert.False(result.Values.Trim);
    Assert.False(result.Values.ExpandTypes);
  }

  [Fact]
  public void Enter_OnTogglesWithNoChanges_Confirms()
  {
    using var console = new TestTuiConsole();
    var prefs = new SolutionPreferences();

    var keys = new FakeKeyReader([K(ConsoleKey.Enter)]);

    var result = OptionsPrompt.Show(prefs, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.False(result.Values!.Recursive);
    Assert.Equal(int.MaxValue, result.Values.MaxDepth);
    Assert.False(result.Values.ExpandTypes);
  }

  [Fact]
  public void Esc_OnToggles_ReturnsQuitImmediately()
  {
    using var console = new TestTuiConsole();
    var prefs = new SolutionPreferences();

    var keys = new FakeKeyReader([K(ConsoleKey.Escape)]);

    var result = OptionsPrompt.Show(prefs, console, keys);

    Assert.Equal(StepResult.Quit, result.Step);
    Assert.Null(result.Values);
  }

  [Fact]
  public void CtrlQ_OnToggles_ReturnsBackImmediately()
  {
    using var console = new TestTuiConsole();
    var prefs = new SolutionPreferences();

    var keys = new FakeKeyReader([CtrlQ()]);

    var result = OptionsPrompt.Show(prefs, console, keys);

    Assert.Equal(StepResult.Back, result.Step);
    Assert.Null(result.Values);
  }

  [Fact]
  public void LimitDepth_On_NumericValidationRejectsNonNumeric_ThenAccepts()
  {
    using var console = new TestTuiConsole();
    var prefs = new SolutionPreferences();

    // Toggles: Space (Recursive), Down, Space (LimitDepth), Enter.
    // MaxDepth: clear default '3' (Backspace), type 'a', Enter (rejected),
    //          Backspace 'a', type '7', Enter (accepted).
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Backspace, '\b'),
      Char('a'),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Backspace, '\b'),
      Char('7'),
      K(ConsoleKey.Enter),
    ]);

    var result = OptionsPrompt.Show(prefs, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.True(result.Values!.Recursive);
    Assert.Equal(7, result.Values.MaxDepth);
  }

  [Fact]
  public void CtrlQ_OnMaxDepth_ReturnsToToggles()
  {
    using var console = new TestTuiConsole();
    var prefs = new SolutionPreferences();

    // Toggles: Space (Recursive), Down, Space (LimitDepth), Enter -> MaxDepth.
    // MaxDepth: Ctrl+Q -> back to Toggles.
    // Toggles: Ctrl+Q -> back.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      CtrlQ(),
      CtrlQ(),
    ]);

    var result = OptionsPrompt.Show(prefs, console, keys);

    Assert.Equal(StepResult.Back, result.Step);
  }

  [Fact]
  public void Esc_OnMaxDepth_ReturnsQuit()
  {
    using var console = new TestTuiConsole();
    var prefs = new SolutionPreferences();

    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Escape),
    ]);

    var result = OptionsPrompt.Show(prefs, console, keys);

    Assert.Equal(StepResult.Quit, result.Step);
  }

  [Fact]
  public void InitialValues_PrePopulated_FromPreferences()
  {
    using var console = new TestTuiConsole();

    var prefs = new SolutionPreferences
    {
      Defaults = new PreferenceDefaults
      {
        Recursive = true,
        LimitDepth = true,
        MaxDepth = 5,
        IncludeCompiled = true,
        Trim = true,
        ExpandTypes = true,
      },
    };

    // Toggles: Enter (accept all pre-set toggles).
    // MaxDepth: Enter (accept default 5 from prefs).
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
    ]);

    var result = OptionsPrompt.Show(prefs, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.True(result.Values!.Recursive);
    Assert.Equal(5, result.Values.MaxDepth);
    Assert.True(result.Values.IncludeCompiled);
    Assert.True(result.Values.Trim);
    Assert.True(result.Values.ExpandTypes);
  }

  [Fact]
  public void Toggle_ExpandTypes_FlowsIntoResult()
  {
    using var console = new TestTuiConsole();
    var prefs = new SolutionPreferences();

    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
    ]);

    var result = OptionsPrompt.Show(prefs, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.NotNull(result.Values);
    Assert.True(result.Values!.ExpandTypes);
  }

  [Fact]
  public void Toggle_LimitDepth_LabelMentionsMaxDepth()
  {
    using var console = new TestTuiConsole();
    var prefs = new SolutionPreferences
    {
      Defaults = new PreferenceDefaults { MaxDepth = 5 },
    };

    var keys = new FakeKeyReader([K(ConsoleKey.Enter)]);

    OptionsPrompt.Show(prefs, console, keys);

    Assert.Contains("Limit recursion depth", console.Output, StringComparison.Ordinal);
    Assert.Contains("Max depth", console.Output, StringComparison.Ordinal);
    Assert.Contains("5", console.Output, StringComparison.Ordinal);
  }

  [Fact]
  public void MaxDepth_InputLabel_MentionsLimitRecursionDepth()
  {
    using var console = new TestTuiConsole();
    var prefs = new SolutionPreferences();

    // Toggles: Space (Recursive), Down, Space (LimitDepth), Enter -> MaxDepth screen renders label.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
    ]);

    OptionsPrompt.Show(prefs, console, keys);

    Assert.Contains("Max depth for", console.Output, StringComparison.Ordinal);
    Assert.Contains("Limit recursion depth", console.Output, StringComparison.Ordinal);
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
