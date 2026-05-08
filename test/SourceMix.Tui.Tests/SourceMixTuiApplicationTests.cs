using System.IO.Abstractions.TestingHelpers;
using Spectre.Console.Testing;

namespace Hj.SourceMix.Tui.Tests;

public sealed class SourceMixTuiApplicationTests
{
  [Fact]
  public async Task RunAsync_NoSolutionFound_ReturnsOneAsync()
  {
    var fs = new MockFileSystem();
    fs.Directory.CreateDirectory("/empty");
    using var ansi = new TestConsole();
    using var tui = new TestTuiConsole();
    var keys = new FakeKeyReader([]);

    var exit = await SourceMixTuiApplication.RunAsync(fs, "/empty", ansi, tui, keys);

    Assert.Equal(1, exit);
    Assert.Contains("No solution file", ansi.Output, StringComparison.Ordinal);
  }

  [Fact]
  public async Task RunAsync_NoCsFiles_ReturnsZeroCleanlyAsync()
  {
    var fs = new MockFileSystem();
    fs.Directory.CreateDirectory("/repo");
    fs.File.WriteAllText("/repo/Foo.slnx", string.Empty);
    using var ansi = new TestConsole();
    using var tui = new TestTuiConsole();
    var keys = new FakeKeyReader([]);

    var exit = await SourceMixTuiApplication.RunAsync(fs, "/repo", ansi, tui, keys);

    Assert.Equal(0, exit);
    Assert.Contains("No .cs files", ansi.Output, StringComparison.Ordinal);
  }

  [Fact]
  public async Task RunAsync_HappyPath_WritesOutputContainingProcessedSourceAsync()
  {
    var fs = MakeProjectFs(out var outputPath);
    using var ansi = new TestConsole();
    using var tui = new TestTuiConsole();
    var keys = new FakeKeyReader(HappyPathKeys());

    var exit = await SourceMixTuiApplication.RunAsync(fs, "/repo", ansi, tui, keys);

    Assert.Equal(0, exit);
    Assert.True(fs.File.Exists(outputPath));
    var written = fs.File.ReadAllText(outputPath);
    Assert.Contains("class Foo", written, StringComparison.Ordinal);
    Assert.Contains("# Source code", written, StringComparison.Ordinal);
    Assert.DoesNotContain("SourceMix —", ansi.Output, StringComparison.Ordinal);
  }

  [Fact]
  public async Task RunAsync_WizardCancelled_ReturnsZeroWithCancelledMessageAsync()
  {
    var fs = MakeProjectFs(out _);
    using var ansi = new TestConsole();
    using var tui = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.Escape)]);

    var exit = await SourceMixTuiApplication.RunAsync(fs, "/repo", ansi, tui, keys);

    Assert.Equal(0, exit);
    Assert.Contains("Cancelled", ansi.Output, StringComparison.Ordinal);
  }

  [Fact]
  public async Task RunAsync_OutputExists_Overwrite_ReplacesFileContentsAsync()
  {
    var fs = MakeProjectFs(out var outputPath);
    fs.File.WriteAllText(outputPath, "PRIOR_CONTENT_\n");

    using var ansi = new TestConsole();
    ansi.Profile.Capabilities.Interactive = true;
    ansi.Input.PushKey(ConsoleKey.Enter); // cursor on "Overwrite"

    using var tui = new TestTuiConsole();
    var keys = new FakeKeyReader(HappyPathKeys());

    var exit = await SourceMixTuiApplication.RunAsync(fs, "/repo", ansi, tui, keys);

    Assert.Equal(0, exit);
    var written = fs.File.ReadAllText(outputPath);
    Assert.DoesNotContain("PRIOR_CONTENT_", written, StringComparison.Ordinal);
    Assert.Contains("class Foo", written, StringComparison.Ordinal);
  }

  [Fact]
  public async Task RunAsync_OutputExists_Append_KeepsPriorContentAsync()
  {
    var fs = MakeProjectFs(out var outputPath);
    fs.File.WriteAllText(outputPath, "PRIOR_CONTENT_\n");

    using var ansi = new TestConsole();
    ansi.Profile.Capabilities.Interactive = true;
    ansi.Input.PushKey(ConsoleKey.DownArrow); // move to "Append"
    ansi.Input.PushKey(ConsoleKey.Enter);

    using var tui = new TestTuiConsole();
    var keys = new FakeKeyReader(HappyPathKeys());

    var exit = await SourceMixTuiApplication.RunAsync(fs, "/repo", ansi, tui, keys);

    Assert.Equal(0, exit);
    var written = fs.File.ReadAllText(outputPath);
    Assert.Contains("PRIOR_CONTENT_", written, StringComparison.Ordinal);
    Assert.Contains("class Foo", written, StringComparison.Ordinal);
  }

  [Fact]
  public async Task RunAsync_OutputExists_Cancel_DoesNotModifyFileAsync()
  {
    var fs = MakeProjectFs(out var outputPath);
    const string Prior = "PRIOR_CONTENT_\n";
    fs.File.WriteAllText(outputPath, Prior);

    using var ansi = new TestConsole();
    ansi.Profile.Capabilities.Interactive = true;
    ansi.Input.PushKey(ConsoleKey.DownArrow);
    ansi.Input.PushKey(ConsoleKey.DownArrow);
    ansi.Input.PushKey(ConsoleKey.Enter); // "Cancel"

    using var tui = new TestTuiConsole();
    var keys = new FakeKeyReader(HappyPathKeys());

    var exit = await SourceMixTuiApplication.RunAsync(fs, "/repo", ansi, tui, keys);

    Assert.Equal(0, exit);
    Assert.Equal(Prior, fs.File.ReadAllText(outputPath));
    Assert.Contains("Cancelled", ansi.Output, StringComparison.Ordinal);
  }

  [Fact]
  public async Task RunAsync_InvalidOutputDirectory_ValidatorRejectsThenAcceptsValidPathAsync()
  {
    var fs = MakeProjectFs(out _);
    using var ansi = new TestConsole();
    using var tui = new TestTuiConsole();

    // Same as HappyPath but on the Output step we backspace the default,
    // type a path whose directory does NOT exist, press Enter (validator rejects),
    // then backspace and type a valid path under /repo, Enter.
    var defaultOutput = DefaultOutputPath();
    var keysList = new List<ConsoleKeyInfo>
    {
      K(ConsoleKey.Spacebar, ' '),  // Files — toggle select
      K(ConsoleKey.Enter),          // Files — confirm
      K(ConsoleKey.Enter),          // Options toggles — confirm
    };

    // Backspace the default initial value entirely.
    for (var i = 0; i < defaultOutput.Length; i++)
    {
      keysList.Add(K(ConsoleKey.Backspace, '\b'));
    }

    // Type an invalid path (directory /nope does not exist in mock fs).
    foreach (var c in "/nope/x.md")
    {
      keysList.Add(Char(c));
    }

    keysList.Add(K(ConsoleKey.Enter)); // rejected

    // Backspace the bad path entirely (/nope/x.md).
    for (var i = 0; i < "/nope/x.md".Length; i++)
    {
      keysList.Add(K(ConsoleKey.Backspace, '\b'));
    }

    foreach (var c in "/repo/out2.md")
    {
      keysList.Add(Char(c));
    }

    keysList.Add(K(ConsoleKey.Enter)); // accepted

    // PromptStep: Enter (skip).
    keysList.Add(K(ConsoleKey.Enter));

    var keys = new FakeKeyReader(keysList);

    var exit = await SourceMixTuiApplication.RunAsync(fs, "/repo", ansi, tui, keys);

    Assert.Equal(0, exit);
    Assert.True(fs.File.Exists("/repo/out2.md"));
  }

  [Fact]
  public async Task RunAsync_FilesStep_NoSelection_ReturnsToOptions_ThenEscExitsCleanlyAsync()
  {
    var fs = MakeProjectFs(out _);
    using var ansi = new TestConsole();
    using var tui = new TestTuiConsole();

    // Wizard order: Files -> Options -> Prompt.
    // 1) Files: Enter, Enter, Enter, Enter (4x confirm with no selection -> loops in FilesStep).
    // 2) Files: Esc -> wizard exits as Cancelled.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Enter),
      K(ConsoleKey.Escape),
      K(ConsoleKey.Escape),
    ]);

    var exit = await SourceMixTuiApplication.RunAsync(fs, "/repo", ansi, tui, keys);

    Assert.Equal(0, exit);
    Assert.Contains("Cancelled", ansi.Output, StringComparison.Ordinal);
  }

  [Fact]
  public async Task RunAsync_WizardQuit_PrintsCancelledAndDoesNotWriteFileAsync()
  {
    var fs = MakeProjectFs(out var outputPath);
    using var ansi = new TestConsole();
    using var tui = new TestTuiConsole();
    // Esc on the very first step (Files) triggers Quit.
    var keys = new FakeKeyReader([K(ConsoleKey.Escape)]);

    var exit = await SourceMixTuiApplication.RunAsync(fs, "/repo", ansi, tui, keys);

    Assert.Equal(0, exit);
    Assert.Contains("Cancelled", ansi.Output, StringComparison.Ordinal);
    Assert.False(fs.File.Exists(outputPath));
  }

  private static MockFileSystem MakeProjectFs(out string outputPath)
  {
    var fs = new MockFileSystem();
    fs.Directory.CreateDirectory("/repo");

    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    if (!string.IsNullOrEmpty(home))
    {
      fs.Directory.CreateDirectory(home);
    }

    fs.File.WriteAllText("/repo/Foo.slnx", string.Empty);
    fs.File.WriteAllText(
      "/repo/Foo.cs",
      "namespace N;\npublic class Foo\n{\n  public int X => 1;\n}\n");

    outputPath = DefaultOutputPath();

    return fs;
  }

  private static string DefaultOutputPath()
  {
    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    return Path.Combine(home, "sourcemix.md");
  }

  private static ConsoleKeyInfo[] HappyPathKeys() =>
  [
    K(ConsoleKey.Spacebar, ' '),          // Files — toggle select
    K(ConsoleKey.Enter),                  // Files — confirm
    K(ConsoleKey.Enter),                  // Options toggles — confirm
    K(ConsoleKey.Enter),                  // Output — accept default
    K(ConsoleKey.Enter),                  // PromptSelector — skip
  ];

  private static ConsoleKeyInfo K(ConsoleKey key, char ch = '\0')
    => new(ch, key, shift: false, alt: false, control: false);

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
