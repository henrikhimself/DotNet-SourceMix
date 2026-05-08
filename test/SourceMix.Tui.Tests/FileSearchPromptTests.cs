#pragma warning disable CA1861 // CA1861 noisy in xUnit Assert.Equal calls.

namespace Hj.SourceMix.Tui.Tests;

public sealed class FileSearchPromptTests
{
  [Fact]
  public void Filter_TypingChars_ReducesVisibleItems()
  {
    var files = MakeFiles("alpha.cs", "beta.cs", "gamma.cs", "alphabet.cs");
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      Char('a'),
      Char('l'),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
    ]);

    var pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var result = FileSearchPrompt.Show(files, pinned, console, keys);

    Assert.Equal(new[] { Path("alpha.cs") }, result.SelectedPaths);
  }

  [Fact]
  public void Space_TogglesSelection_InSearchView()
  {
    var files = MakeFiles("a.cs", "b.cs", "c.cs");
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
    ]);

    var result = FileSearchPrompt.Show(files, new HashSet<string>(StringComparer.OrdinalIgnoreCase), console, keys);

    Assert.Equal(new[] { Path("a.cs"), Path("c.cs") }, result.SelectedPaths);
  }

  [Fact]
  public void Tab_RewritesAppHeading_OnEachTransition()
  {
    var files = MakeFiles("a.cs", "b.cs");
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),          // select "a.cs"
      K(ConsoleKey.Tab),                    // Search -> Pinned (heading 1)
      K(ConsoleKey.Tab),                    // Pinned -> Selected (heading 2)
      K(ConsoleKey.Enter),                  // confirm
    ]);

    FileSearchPrompt.Show(files, new HashSet<string>(StringComparer.OrdinalIgnoreCase), console, keys);

    var occurrences = CountOccurrences(console.Output, "SourceMix");
    Assert.True(occurrences >= 2, $"Expected SourceMix heading to be re-emitted on every Tab transition, but found {occurrences} occurrences.");
  }

  [Fact]
  public void ResizeRedraw_RestoresAppHeading()
  {
    var files = MakeFiles("a.cs", "b.cs");
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([]);
    keys.Enqueue(K(ConsoleKey.Spacebar, ' '));
    keys.EnqueueIdle();
    keys.EnqueueAction(() => console.SetWindowHeight(12));
    keys.Enqueue(K(ConsoleKey.Enter));

    var result = FileSearchPrompt.Show(files, new HashSet<string>(StringComparer.OrdinalIgnoreCase), console, keys);

    Assert.Equal(new[] { Path("a.cs") }, result.SelectedPaths);
    Assert.True(console.ClearScreenCallCount >= 1);
    Assert.Contains("SourceMix", console.Output, StringComparison.Ordinal);
  }

  [Fact]
  public void Tab_CyclesSearch_To_Pinned_To_Selected_To_Search()
  {
    var files = MakeFiles("a.cs", "b.cs");
    using var console = new TestTuiConsole();
    // Tab three times to cycle back to Search, then Enter confirms.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),         // select "a.cs" in Search
      K(ConsoleKey.Tab),                    // -> Pinned
      K(ConsoleKey.Tab),                    // -> Selected
      K(ConsoleKey.Tab),                    // -> Search
      K(ConsoleKey.Enter),                  // confirm
    ]);

    var result = FileSearchPrompt.Show(files, new HashSet<string>(StringComparer.OrdinalIgnoreCase), console, keys);

    Assert.Equal(new[] { Path("a.cs") }, result.SelectedPaths);
  }

  [Fact]
  public void CtrlP_TogglesPin_InSearch_AndPinnedAppearsInPinnedView()
  {
    var files = MakeFiles("a.cs", "b.cs");
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.DownArrow),              // cursor on "b.cs"
      K(ConsoleKey.P, 'p', ctrl: true),     // pin "b.cs"
      K(ConsoleKey.Tab),                    // -> Pinned view
      K(ConsoleKey.Enter),                  // confirm from Pinned
    ]);

    var result = FileSearchPrompt.Show(files, new HashSet<string>(StringComparer.OrdinalIgnoreCase), console, keys);

    Assert.Contains(Path("b.cs"), result.PinnedPaths);
    Assert.Contains(Path("b.cs"), result.SelectedPaths);
  }

  [Fact]
  public void CtrlU_ClearsSearchTerm_NotSelections()
  {
    var files = MakeFiles("alpha.cs", "beta.cs", "gamma.cs");
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),                // select "alpha.cs"
      Char('b'),                                  // filter narrows to "beta.cs"
      K(ConsoleKey.U, 'u', ctrl: true),           // clear search term
      K(ConsoleKey.Enter),                        // confirm; selection remains
    ]);

    var result = FileSearchPrompt.Show(files, new HashSet<string>(StringComparer.OrdinalIgnoreCase), console, keys);

    Assert.Equal(new[] { Path("alpha.cs") }, result.SelectedPaths);
  }

  [Fact]
  public void Esc_ReturnsEmptySelection_AndDoesNotPersistPins()
  {
    var files = MakeFiles("a.cs", "b.cs");
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.P, 'p', ctrl: true),
      K(ConsoleKey.Escape),
    ]);

    var result = FileSearchPrompt.Show(files, new HashSet<string>(StringComparer.OrdinalIgnoreCase), console, keys);

    Assert.Empty(result.SelectedPaths);
    Assert.Empty(result.PinnedPaths);
  }

  [Fact]
  public void Enter_ReturnsSelectedPaths_InOriginalOrder()
  {
    var files = MakeFiles("zebra.cs", "apple.cs", "mango.cs");
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.DownArrow),              // apple
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.UpArrow),                // zebra
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
    ]);

    var result = FileSearchPrompt.Show(files, new HashSet<string>(StringComparer.OrdinalIgnoreCase), console, keys);

    // Expect original (input) order, not selection order.
    Assert.Equal(new[] { Path("zebra.cs"), Path("apple.cs") }, result.SelectedPaths);
  }

  [Fact]
  public void InitialPinnedPaths_AreSelected_AndReturnedOnConfirm()
  {
    var files = MakeFiles("a.cs", "b.cs");
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.Enter)]);
    var pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path("b.cs") };

    var result = FileSearchPrompt.Show(files, pinned, console, keys);

    Assert.Contains(Path("b.cs"), result.SelectedPaths);
    Assert.Contains(Path("b.cs"), result.PinnedPaths);
  }

  [Fact]
  public void Pinned_CtrlP_Unpins_ButKeepsSelection()
  {
    var files = MakeFiles("a.cs", "b.cs");
    using var console = new TestTuiConsole();
    var pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path("a.cs"), Path("b.cs") };

    // Tab -> Pinned. Ctrl+P on first row (a.cs) unpins; selection must remain. Tab Tab -> Search. Enter.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Tab),
      K(ConsoleKey.P, 'p', ctrl: true),
      K(ConsoleKey.Tab),
      K(ConsoleKey.Tab),
      K(ConsoleKey.Enter),
    ]);

    var result = FileSearchPrompt.Show(files, pinned, console, keys);

    Assert.DoesNotContain(Path("a.cs"), result.PinnedPaths);
    Assert.Contains(Path("a.cs"), result.SelectedPaths);
    Assert.Contains(Path("b.cs"), result.PinnedPaths);
    Assert.Contains(Path("b.cs"), result.SelectedPaths);
  }

  [Fact]
  public void Pinned_CtrlP_Repins_RestoresPinWithoutChangingSelection()
  {
    var files = MakeFiles("a.cs", "b.cs");
    using var console = new TestTuiConsole();
    var pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path("a.cs") };

    // Tab -> Pinned. Ctrl+P unpins a.cs (Pinned view becomes empty so cursor stays at 0 with no item).
    // Tab Tab -> Search. Down to a.cs, Ctrl+P re-pins. Enter.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Tab),
      K(ConsoleKey.P, 'p', ctrl: true),
      K(ConsoleKey.Tab),
      K(ConsoleKey.Tab),
      K(ConsoleKey.P, 'p', ctrl: true),
      K(ConsoleKey.Enter),
    ]);

    var result = FileSearchPrompt.Show(files, pinned, console, keys);

    Assert.Contains(Path("a.cs"), result.PinnedPaths);
    Assert.Contains(Path("a.cs"), result.SelectedPaths);
  }

  [Fact]
  public void Selected_Space_DeselectsButKeepsPin()
  {
    var files = MakeFiles("a.cs", "b.cs");
    using var console = new TestTuiConsole();
    var pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path("a.cs") };

    // Tab Tab -> Selected. Space deselects (pin remains). Tab -> Search. Enter.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Tab),
      K(ConsoleKey.Tab),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Tab),
      K(ConsoleKey.Enter),
    ]);

    var result = FileSearchPrompt.Show(files, pinned, console, keys);

    Assert.DoesNotContain(Path("a.cs"), result.SelectedPaths);
    Assert.Contains(Path("a.cs"), result.PinnedPaths);
  }

  [Fact]
  public void Selected_Space_ReselectsAfterDeselect()
  {
    var files = MakeFiles("a.cs", "b.cs");
    using var console = new TestTuiConsole();
    var pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path("a.cs") };

    // Tab Tab -> Selected. Space deselects, Space reselects, Enter.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Tab),
      K(ConsoleKey.Tab),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
    ]);

    var result = FileSearchPrompt.Show(files, pinned, console, keys);

    Assert.Contains(Path("a.cs"), result.SelectedPaths);
    Assert.Contains(Path("a.cs"), result.PinnedPaths);
  }

  [Fact]
  public void Search_DeselectPinned_PersistsAcrossTab()
  {
    var files = MakeFiles("a.cs", "b.cs");
    using var console = new TestTuiConsole();
    var pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path("a.cs") };

    // Search: Space (deselect a.cs, also unpins). Tab Tab Tab -> Search. Enter.
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Tab),
      K(ConsoleKey.Tab),
      K(ConsoleKey.Tab),
      K(ConsoleKey.Enter),
    ]);

    var result = FileSearchPrompt.Show(files, pinned, console, keys);

    Assert.DoesNotContain(Path("a.cs"), result.SelectedPaths);
    Assert.DoesNotContain(Path("a.cs"), result.PinnedPaths);
  }

  private static IReadOnlyList<CsFile> MakeFiles(params string[] names)
    => [.. names.Select(n => new CsFile(Path(n), n))];

  private static string Path(string name) => $"/repo/{name}";

  private static ConsoleKeyInfo K(ConsoleKey key, char ch = '\0', bool ctrl = false)
    => new(ch, key, shift: false, alt: false, control: ctrl);

  private static ConsoleKeyInfo Char(char c)
  {
    var key = char.IsLetter(c)
      ? (ConsoleKey)char.ToUpperInvariant(c)
      : ConsoleKey.Oem1;

    return new ConsoleKeyInfo(c, key, shift: false, alt: false, control: false);
  }

  private static int CountOccurrences(string haystack, string needle)
  {
    var count = 0;
    var index = 0;

    while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
    {
      count++;
      index += needle.Length;
    }

    return count;
  }
}
