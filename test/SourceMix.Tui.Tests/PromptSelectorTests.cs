using Hj.SourceMix.Core;

namespace Hj.SourceMix.Tui.Tests;

public sealed class PromptSelectorTests
{
  [Fact]
  public void Show_ListsSkipPlusBuiltInsPlusNew()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.Enter)]);

    var result = PromptSelector.Show(new GlobalPreferences(), defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.Null(result.PromptKey);
    Assert.Contains("(none", console.Output, StringComparison.OrdinalIgnoreCase);

    foreach (var key in BuiltInPrompts.All.Keys)
    {
      Assert.Contains(key, console.Output, StringComparison.OrdinalIgnoreCase);
    }

    Assert.Contains("New custom prompt", console.Output, StringComparison.Ordinal);
  }

  [Fact]
  public void Show_DefaultPromptKey_PreselectsSavedEntry()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.Enter)]);

    var result = PromptSelector.Show(new GlobalPreferences(), defaultPromptKey: "code-review", console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.Equal("code-review", result.PromptKey);
    Assert.NotNull(result.PromptText);
  }

  [Fact]
  public void Show_SelectingSkip_ReturnsConfirmWithNoPrompt()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.Enter)]);

    var result = PromptSelector.Show(new GlobalPreferences(), defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.Null(result.PromptKey);
    Assert.Null(result.PromptText);
  }

  [Fact]
  public void Show_SelectingBuiltIn_PopulatesPromptKeyAndText()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.DownArrow), K(ConsoleKey.Enter)]);

    var result = PromptSelector.Show(new GlobalPreferences(), defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.NotNull(result.PromptKey);
    Assert.NotNull(result.PromptText);
    Assert.True(BuiltInPrompts.All.ContainsKey(result.PromptKey!));
  }

  [Fact]
  public void Show_NewCustomPrompt_CtrlQOnName_ReturnsToPicker_ThenCtrlQExits()
  {
    using var console = new TestTuiConsole();
    var prefs = new GlobalPreferences();

    // Cursor moves to last entry (+ New custom prompt) via End. Press Enter.
    // Then the name TextInput appears: press Ctrl+Q -> recurses back into PromptSelector.Show.
    // Ctrl+Q on the picker -> Step.Back.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.End),
      K(ConsoleKey.Enter),
      CtrlQ(),
      CtrlQ(),
    ]);

    var result = PromptSelector.Show(prefs, defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Back, result.Step);
    Assert.Empty(result.Preferences.CustomPrompts);
  }

  [Fact]
  public void Show_NewCustomPrompt_EscOnName_ReturnsQuit()
  {
    using var console = new TestTuiConsole();
    var prefs = new GlobalPreferences();

    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.End),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Escape),
    ]);

    var result = PromptSelector.Show(prefs, defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Quit, result.Step);
    Assert.Empty(result.Preferences.CustomPrompts);
  }

  [Fact]
  public void Show_EscOnPicker_ReturnsQuit()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.Escape)]);

    var result = PromptSelector.Show(new GlobalPreferences(), defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Quit, result.Step);
  }

  [Fact]
  public void Show_NewCustomPrompt_CtrlQOnText_RePromptsName()
  {
    using var console = new TestTuiConsole();
    var prefs = new GlobalPreferences();

    // Pick "+ New custom prompt".
    // Type name "myp" + Enter.
    // Ctrl+Q on text -> loops back to name input (which starts blank).
    // Type "q" + Enter.
    // Type text "hi" + Enter.
    // Picker re-appears with new entry; navigate End (+ New) then Up (lands on "q") + Enter.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.End),
      K(ConsoleKey.Enter),
      Char('m'), Char('y'), Char('p'),
      K(ConsoleKey.Enter),
      CtrlQ(),
      Char('q'),
      K(ConsoleKey.Enter),
      Char('h'), Char('i'),
      K(ConsoleKey.Enter),
      K(ConsoleKey.End),
      K(ConsoleKey.UpArrow),
      K(ConsoleKey.Enter),
    ]);

    var result = PromptSelector.Show(prefs, defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.Equal("q", result.PromptKey);
    Assert.Equal("hi", result.PromptText);
    Assert.Contains("q", result.Preferences.CustomPrompts.Keys);
  }

  [Fact]
  public void Show_NewCustomPrompt_EscOnText_ReturnsQuit()
  {
    using var console = new TestTuiConsole();
    var prefs = new GlobalPreferences();

    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.End),
      K(ConsoleKey.Enter),
      Char('m'),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Escape),
    ]);

    var result = PromptSelector.Show(prefs, defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Quit, result.Step);
  }

  [Fact]
  public void Show_NewCustomPrompt_Confirm_PersistsIntoGlobalPreferences()
  {
    using var console = new TestTuiConsole();
    var prefs = new GlobalPreferences();

    // After creation, picker re-appears. End lands on "+ New"; Up lands on "ab" (the new custom entry).
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.End),
      K(ConsoleKey.Enter),
      Char('a'), Char('b'),
      K(ConsoleKey.Enter),
      Char('x'), Char('y'),
      K(ConsoleKey.Enter),
      K(ConsoleKey.End),
      K(ConsoleKey.UpArrow),
      K(ConsoleKey.Enter),
    ]);

    var result = PromptSelector.Show(prefs, defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.Equal("ab", result.PromptKey);
    Assert.Equal("xy", result.PromptText);
    Assert.True(result.Preferences.CustomPrompts.TryGetValue("ab", out var stored));
    Assert.Equal("xy", stored);

    // Original input prefs untouched (record returned a fresh copy).
    Assert.Empty(prefs.CustomPrompts);
  }

  [Fact]
  public void Show_NewCustomPrompt_AfterCreation_CtrlQOnPicker_ReturnsBackWithPersistedPrompt()
  {
    using var console = new TestTuiConsole();
    var prefs = new GlobalPreferences();

    // Create custom prompt "ab"/"xy", then Ctrl+Q on the re-shown picker -> Back.
    // The new prompt remains persisted in the returned preferences even though the user
    // did not select it for this run.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.End),
      K(ConsoleKey.Enter),
      Char('a'), Char('b'),
      K(ConsoleKey.Enter),
      Char('x'), Char('y'),
      K(ConsoleKey.Enter),
      CtrlQ(),
    ]);

    var result = PromptSelector.Show(prefs, defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Back, result.Step);
    Assert.Null(result.PromptKey);
    Assert.Null(result.PromptText);
    Assert.True(result.Preferences.CustomPrompts.TryGetValue("ab", out var stored));
    Assert.Equal("xy", stored);
  }

  [Fact]
  public void Show_CtrlD_OnCustomPrompt_WithConfirm_RemovesIt()
  {
    using var console = new TestTuiConsole();
    var prefs = new GlobalPreferences
    {
      CustomPrompts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
      {
        ["mine"] = "hello",
      },
    };

    // Cursor starts at 0 (Skip); navigate to the custom entry "mine" which sits after built-ins.
    // Sequence: End -> "+ New", UpArrow -> "mine"; Ctrl+D opens confirmation; type 'y' Enter; Enter.
    // After Ctrl+D + 'y' Enter the entry is removed; Ctrl+Q exits the picker so we don't
    // accidentally land on "+ New" (which would block on a follow-up TextInputPrompt).
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.End),
      K(ConsoleKey.UpArrow),
      CtrlD(),
      Char('y'),
      K(ConsoleKey.Enter),
      CtrlQ(),
    ]);

    var result = PromptSelector.Show(prefs, defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Back, result.Step);
    Assert.False(result.Preferences.CustomPrompts.ContainsKey("mine"));
  }

  [Fact]
  public void Show_CtrlD_OnCustomPrompt_WithoutConfirm_KeepsIt()
  {
    using var console = new TestTuiConsole();
    var prefs = new GlobalPreferences
    {
      CustomPrompts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
      {
        ["mine"] = "hello",
      },
    };

    // End -> "+ New", UpArrow -> "mine", Ctrl+D, just Enter (empty answer = not 'y'), then Enter on "mine".
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.End),
      K(ConsoleKey.UpArrow),
      CtrlD(),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
    ]);

    var result = PromptSelector.Show(prefs, defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.Equal("mine", result.PromptKey);
    Assert.True(result.Preferences.CustomPrompts.ContainsKey("mine"));
  }

  [Fact]
  public void Show_CtrlD_OnBuiltInPrompt_IsIgnored()
  {
    using var console = new TestTuiConsole();
    var prefs = new GlobalPreferences();

    // Move to first built-in (DownArrow), Ctrl+D (no confirmation should appear), Enter selects that built-in.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.DownArrow),
      CtrlD(),
      K(ConsoleKey.Enter),
    ]);

    var result = PromptSelector.Show(prefs, defaultPromptKey: null, console, keys);

    Assert.Equal(StepResult.Confirm, result.Step);
    Assert.NotNull(result.PromptKey);
    Assert.True(BuiltInPrompts.All.ContainsKey(result.PromptKey!));
  }

  private static ConsoleKeyInfo CtrlD()
    => new('\x04', ConsoleKey.D, shift: false, alt: false, control: true);

  private static ConsoleKeyInfo K(ConsoleKey key, char ch = '\0')
    => new(ch, key, shift: false, alt: false, control: false);

  private static ConsoleKeyInfo CtrlQ()
    => new('\x11', ConsoleKey.Q, shift: false, alt: false, control: true);

  private static ConsoleKeyInfo Char(char c)
  {
    var key = char.IsLetter(c)
      ? (ConsoleKey)char.ToUpperInvariant(c)
      : ConsoleKey.Oem1;

    return new ConsoleKeyInfo(c, key, shift: false, alt: false, control: false);
  }
}
