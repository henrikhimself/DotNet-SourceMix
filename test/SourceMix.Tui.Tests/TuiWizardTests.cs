#pragma warning disable CA1861
using Hj.SourceMix.Core;

namespace Hj.SourceMix.Tui.Tests;

public sealed class TuiWizardTests
{
  private static readonly IReadOnlyList<CsFile> _sampleFiles =
  [
    new CsFile(FullPath: "/repo/Foo.cs", RelativePath: "Foo.cs"),
    new CsFile(FullPath: "/repo/Bar.cs", RelativePath: "Bar.cs"),
  ];

  private static readonly IReadOnlyList<Skill> _sampleSkills =
  [
    new Skill("alpha", "alpha.skill.md"),
    new Skill("beta", "beta.skill.md"),
  ];

  [Fact]
  public void EveryStep_RendersYellowSourceMixHeading()
  {
    using var console = new TestTuiConsole();
    // Quit on Files step, but the Files step still renders once.
    var keys = new FakeKeyReader([K(ConsoleKey.Escape)]);

    RunWizard(console, keys);

    Assert.Contains("SourceMix", console.Output, StringComparison.Ordinal);
  }

  [Fact]
  public void Esc_OnFilesStep_QuitsImmediately()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.Escape)]);

    var result = RunWizard(console, keys);

    Assert.False(result.Confirmed);
    Assert.True(result.Quit);
  }

  [Fact]
  public void CtrlQ_OnFilesStep_ExitsUnconfirmed()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([CtrlQ()]);

    var result = RunWizard(console, keys);

    Assert.False(result.Confirmed);
    Assert.False(result.Quit);
  }

  [Fact]
  public void CtrlQ_OnOptionsStep_ReturnsToFilesStep_AndExits()
  {
    using var console = new TestTuiConsole();
    // 1) Files: Space (select Foo.cs), Enter -> Options.
    // 2) Options toggles: Ctrl+Q -> back to Files.
    // 3) Files: Ctrl+Q -> Exit.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      CtrlQ(),
      CtrlQ(),
    ]);

    var result = RunWizard(console, keys);

    Assert.False(result.Confirmed);
    Assert.False(result.Quit);
  }

  [Fact]
  public void Esc_OnOptionsStep_QuitsImmediately()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Escape),
    ]);

    var result = RunWizard(console, keys);

    Assert.False(result.Confirmed);
    Assert.True(result.Quit);
  }

  [Fact]
  public void CtrlQ_OnPromptStep_ReturnsToOptionsStep_AndExits()
  {
    using var console = new TestTuiConsole();
    // 1) Files: Space, Enter -> Options.
    // 2) Options toggles: Enter (no toggles changed) -> Output.
    // 3) Output: Enter -> Prompt.
    // 4) Prompt: Ctrl+Q -> back to Output.
    // 5) Output: Ctrl+Q -> Options. Options: Ctrl+Q -> Files. Files: Ctrl+Q -> Exit.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      CtrlQ(),
      CtrlQ(),
      CtrlQ(),
      CtrlQ(),
    ]);

    var result = RunWizard(console, keys);

    Assert.False(result.Confirmed);
    Assert.False(result.Quit);
  }

  [Fact]
  public void Esc_OnPromptStep_QuitsImmediately()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Escape),
    ]);

    var result = RunWizard(console, keys);

    Assert.False(result.Confirmed);
    Assert.True(result.Quit);
  }

  [Fact]
  public void CtrlQ_OnSkillsStep_ReturnsToPromptStep()
  {
    using var console = new TestTuiConsole();
    // 1) Files: Space, Enter -> Options.
    // 2) Options: Enter -> Output.
    // 3) Output: Enter -> Prompt.
    // 4) Prompt: Enter -> Skills.
    // 5) Skills: Ctrl+Q -> back to Prompt.
    // 6) Prompt: Ctrl+Q -> Output. Output: Ctrl+Q -> Options. Options: Ctrl+Q -> Files.
    //    Files: Ctrl+Q -> Exit.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      CtrlQ(),
      CtrlQ(),
      CtrlQ(),
      CtrlQ(),
      CtrlQ(),
    ]);

    var result = RunWizard(console, keys);

    Assert.False(result.Confirmed);
    Assert.False(result.Quit);
  }

  [Fact]
  public void Esc_OnSkillsStep_QuitsImmediately()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Escape),
    ]);

    var result = RunWizard(console, keys);

    Assert.False(result.Confirmed);
    Assert.True(result.Quit);
  }

  [Fact]
  public void HappyPath_AllStepsConfirmed_ProducesConfirmedResult()
  {
    using var console = new TestTuiConsole();
    // 1) Files: Space (select Foo.cs), Enter.
    // 2) Options toggles: Enter (no toggles).
    // 3) Output: Enter (accept default).
    // 4) Prompt: Enter (skip).
    // 5) Skills: Space (select alpha), Enter.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
    ]);

    var result = RunWizard(console, keys);

    Assert.True(result.Confirmed);
    Assert.NotNull(result.Options);
    Assert.False(result.Options!.Recursive);
    Assert.Single(result.SelectedFiles);
    Assert.Equal("/repo/Foo.cs", result.SelectedFiles[0]);
    Assert.Single(result.SelectedSkillKeys);
    Assert.Equal("alpha", result.SelectedSkillKeys[0]);
    Assert.Null(result.PromptKey);
  }

  [Fact]
  public void HappyPath_RecursiveOn_FlowsThroughMaxDepth()
  {
    using var console = new TestTuiConsole();
    // 1) Files: Space, Enter.
    // 2) Options toggles: Space (Recursive on), Down, Space (LimitDepth on), Enter.
    // 3) MaxDepth text: Enter (accept default 3).
    // 4) Output: Enter (accept default).
    // 5) Prompt: Enter.
    // 6) Skills: Enter (no skills selected).
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
    ]);

    var result = RunWizard(console, keys);

    Assert.True(result.Confirmed);
    Assert.NotNull(result.Options);
    Assert.True(result.Options!.Recursive);
    Assert.Equal(3, result.Options.MaxDepth);
    Assert.Empty(result.SelectedSkillKeys);
  }

  [Fact]
  public void FilesStep_ConfirmWithZeroSelection_RedrawsWithHint()
  {
    using var console = new TestTuiConsole();
    // 1) Files: Enter (no selection) -> redraw with hint.
    // 2) Files redraw: Esc -> Quit.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Enter),
      K(ConsoleKey.Escape),
    ]);

    var result = RunWizard(console, keys);

    Assert.False(result.Confirmed);
    Assert.True(result.Quit);
    Assert.Contains("Select at least one file", console.Output, StringComparison.Ordinal);
  }

  [Fact]
  public void BackFromPrompt_AfterOptionsCompleted_LandsOnOutputStep()
  {
    using var console = new TestTuiConsole();
    // 1) Files: Space, Enter -> Options.
    // 2) Options Toggles: Enter -> Output.
    // 3) Output: Enter -> Prompt.
    // 4) Prompt: Ctrl+Q -> Output (NOT Options).
    // 5) Output: Enter -> Prompt. Prompt: Enter (skip) -> Skills.
    // 6) Skills: Enter (no selection).
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      CtrlQ(),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
    ]);

    var result = RunWizard(console, keys);

    Assert.True(result.Confirmed);
    Assert.NotNull(result.Options);
  }

  [Fact]
  public void CtrlQ_OnOutputStep_ReturnsToOptionsStep()
  {
    using var console = new TestTuiConsole();
    // 1) Files: Space, Enter -> Options.
    // 2) Options: Enter -> Output.
    // 3) Output: Ctrl+Q -> Options. Options: Ctrl+Q -> Files. Files: Ctrl+Q -> Exit.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      CtrlQ(),
      CtrlQ(),
      CtrlQ(),
    ]);

    var result = RunWizard(console, keys);

    Assert.False(result.Confirmed);
    Assert.False(result.Quit);
  }

  [Fact]
  public void Esc_OnOutputStep_QuitsImmediately()
  {
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Escape),
    ]);

    var result = RunWizard(console, keys);

    Assert.False(result.Confirmed);
    Assert.True(result.Quit);
  }

  private static TuiWizardResult RunWizard(TestTuiConsole console, FakeKeyReader keys)
  {
    var globalPrefs = new GlobalPreferences();
    var prefs = new SolutionPreferences();
    var emptyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var emptySkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    return TuiWizard.Run(
      globalPrefs,
      prefs,
      _sampleFiles,
      emptyFiles,
      _sampleSkills,
      emptySkills,
      console,
      keys);
  }

  private static ConsoleKeyInfo K(ConsoleKey key, char ch = '\0')
    => new(ch, key, shift: false, alt: false, control: false);

  private static ConsoleKeyInfo CtrlQ()
    => new('\x11', ConsoleKey.Q, shift: false, alt: false, control: true);
}
