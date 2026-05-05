#pragma warning disable CA1861 // CA1861 noisy in xUnit Assert.Equal calls.
using Spectre.Console;

namespace Hj.SourceMix.Tui.Tests;

public sealed class ListPickerPromptTests
{
  [Fact]
  public void DownArrow_MovesCursor_AndClampsAtEnd()
  {
    var items = new[] { "a", "b", "c" };
    var picker = MakePicker(items, multi: false);
    var keys = Keys(K(ConsoleKey.DownArrow), K(ConsoleKey.DownArrow), K(ConsoleKey.DownArrow), K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(ListPickerExitReason.Confirmed, result.Reason);
    Assert.Equal(new[] { "c" }, result.Selected);
  }

  [Fact]
  public void UpArrow_ClampsAtStart()
  {
    var items = new[] { "a", "b", "c" };
    var picker = MakePicker(items, multi: false);
    var keys = Keys(K(ConsoleKey.UpArrow), K(ConsoleKey.UpArrow), K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "a" }, result.Selected);
  }

  [Fact]
  public void PageDown_PageUp_MoveByPage()
  {
    var items = Enumerable.Range(0, 30).Select(i => $"item{i:00}").ToArray();
    var picker = MakePicker(items, multi: false);
    var console = new TestTuiConsole(height: 14);
    using var dispose1 = console;
    var keys = Keys(K(ConsoleKey.PageDown), K(ConsoleKey.PageDown), K(ConsoleKey.PageUp), K(ConsoleKey.Enter));

    var result = picker.Show(console, keys);

    Assert.Equal(new[] { "item08" }, result.Selected);
  }

  [Fact]
  public void Home_End_JumpToBounds()
  {
    var items = new[] { "a", "b", "c", "d" };
    var picker = MakePicker(items, multi: false);
    var keys = Keys(K(ConsoleKey.End), K(ConsoleKey.Home), K(ConsoleKey.End), K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "d" }, result.Selected);
  }

  [Fact]
  public void Enter_NoMultiSelect_ReturnsCursorItem()
  {
    var items = new[] { "x", "y", "z" };
    var picker = MakePicker(items, multi: false);
    var keys = Keys(K(ConsoleKey.DownArrow), K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(ListPickerExitReason.Confirmed, result.Reason);
    Assert.Equal(new[] { "y" }, result.Selected);
  }

  [Fact]
  public void Esc_ReturnsQuit_WithEmptySelection()
  {
    var items = new[] { "a", "b" };
    var picker = MakePicker(items, multi: false);
    var keys = Keys(K(ConsoleKey.Escape));

    var result = RunWith(picker, keys);

    Assert.Equal(ListPickerExitReason.Quit, result.Reason);
    Assert.Empty(result.Selected);
  }

  [Fact]
  public void CtrlQ_ReturnsSkipped_WithEmptySelection()
  {
    var items = new[] { "a", "b" };
    var picker = MakePicker(items, multi: false);
    var keys = Keys(K(ConsoleKey.Q, 'q', ctrl: true));

    var result = RunWith(picker, keys);

    Assert.Equal(ListPickerExitReason.Skipped, result.Reason);
    Assert.Empty(result.Selected);
  }

  [Fact]
  public void MultiSelect_Space_TogglesAndReturnsInItemsOrder()
  {
    var items = new[] { "a", "b", "c", "d" };
    var picker = MakePicker(items, multi: true);
    var keys = Keys(
      K(ConsoleKey.End),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Home),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "a", "d" }, result.Selected);
  }

  [Fact]
  public void AllowPin_CtrlP_TogglesPin_AndPersistsAcrossConfirm()
  {
    var items = new[] { "a", "b", "c" };
    var picker = MakePicker(items, multi: false, allowPin: true);
    var keys = Keys(
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.P, 'p', ctrl: true),
      K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(ListPickerExitReason.Confirmed, result.Reason);
    Assert.Contains("b", result.Pinned);
  }

  [Fact]
  public void AllowPin_CtrlP_Twice_RemovesPin()
  {
    var items = new[] { "a", "b" };
    var picker = MakePicker(items, multi: false, allowPin: true);
    var keys = Keys(
      K(ConsoleKey.P, 'p', ctrl: true),
      K(ConsoleKey.P, 'p', ctrl: true),
      K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Empty(result.Pinned);
  }

  [Fact]
  public void Filter_TypingChars_FiltersList_AndCursorResetsToZero()
  {
    var items = new[] { "alpha", "beta", "alphabet", "gamma" };
    var picker = new ListPickerPrompt<string>
    {
      Header = "[bold]Pick[/]",
      Items = items,
      KeySelector = s => s,
      Renderer = (s, state) => $"{(state.IsCursor ? "> " : "  ")}{Markup.Escape(s)}",
      Filter = text => string.IsNullOrEmpty(text)
        ? items
        : [.. items.Where(s => s.Contains(text, StringComparison.OrdinalIgnoreCase))],
    };

    var keys = Keys(
      K(ConsoleKey.End),
      Char('a'),
      Char('l'),
      K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "alpha" }, result.Selected);
  }

  [Fact]
  public void Filter_Backspace_RemovesChar_AndCursorResetsToZero()
  {
    var items = new[] { "alpha", "beta", "alphabet" };
    var picker = new ListPickerPrompt<string>
    {
      Header = "[bold]Pick[/]",
      Items = items,
      KeySelector = s => s,
      Renderer = (s, state) => $"{(state.IsCursor ? "> " : "  ")}{Markup.Escape(s)}",
      Filter = text => string.IsNullOrEmpty(text)
        ? items
        : [.. items.Where(s => s.Contains(text, StringComparison.OrdinalIgnoreCase))],
    };

    var keys = Keys(
      Char('b'),
      Char('e'),
      K(ConsoleKey.Backspace, '\b'),
      K(ConsoleKey.Backspace, '\b'),
      K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "alpha" }, result.Selected);
  }

  [Fact]
  public void NoFilter_TypingChars_Ignored()
  {
    var items = new[] { "a", "b", "c" };
    var picker = MakePicker(items, multi: false);
    var keys = Keys(Char('z'), Char('z'), K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "a" }, result.Selected);
  }

  [Fact]
  public void CtrlU_ClearsFilterText_WhenFilterPresent()
  {
    var items = new[] { "alpha", "beta", "alphabet" };
    var picker = new ListPickerPrompt<string>
    {
      Header = "[bold]Pick[/]",
      Items = items,
      KeySelector = s => s,
      Renderer = (s, state) => $"{(state.IsCursor ? "> " : "  ")}{Markup.Escape(s)}",
      Filter = text => string.IsNullOrEmpty(text)
        ? items
        : [.. items.Where(s => s.Contains(text, StringComparison.OrdinalIgnoreCase))],
    };

    // Type "be" -> filter narrows to "beta"; Ctrl+U clears filter; Enter on first row "alpha".
    var keys = Keys(
      Char('b'),
      Char('e'),
      K(ConsoleKey.U, 'u', ctrl: true),
      K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "alpha" }, result.Selected);
  }

  [Fact]
  public void CtrlU_DoesNotClearSelection_WhenMultiSelect()
  {
    var items = new[] { "a", "b", "c" };
    var picker = MakePicker(items, multi: true);
    var keys = Keys(
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.U, 'u', ctrl: true),
      K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    // Ctrl+U is a no-op when no Filter is configured; selections remain.
    Assert.Equal(new[] { "a", "b" }, result.Selected);
  }

  [Fact]
  public void EmptyItems_Esc_ReturnsCleanly()
  {
    var picker = MakePicker(Array.Empty<string>(), multi: false);
    var keys = Keys(K(ConsoleKey.Escape));

    var result = RunWith(picker, keys);

    Assert.Equal(ListPickerExitReason.Quit, result.Reason);
    Assert.Empty(result.Selected);
  }

  [Fact]
  public void EmptyItems_Enter_ReturnsCleanly()
  {
    var picker = MakePicker(Array.Empty<string>(), multi: false);
    var keys = Keys(K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(ListPickerExitReason.Confirmed, result.Reason);
    Assert.Empty(result.Selected);
  }

  [Fact]
  public void SmallWindow_MaxVisibleAtLeastFive_AndMoreBelowHintShown()
  {
    var items = Enumerable.Range(0, 12).Select(i => $"i{i}").ToArray();
    var picker = MakePicker(items, multi: false);
    var console = new TestTuiConsole(height: 8);
    using var dispose2 = console;
    var keys = Keys(K(ConsoleKey.Enter));

    var result = picker.Show(console, keys);

    Assert.Equal(ListPickerExitReason.Confirmed, result.Reason);
    Assert.Equal(new[] { "i0" }, result.Selected);
    Assert.Contains("more below", console.Output, StringComparison.Ordinal);
  }

  [Fact]
  public void InitialPinned_PreservedOnSkip()
  {
    var items = new[] { "a", "b" };
    var picker = new ListPickerPrompt<string>
    {
      Header = "[bold]Pick[/]",
      Items = items,
      KeySelector = s => s,
      Renderer = (s, state) => $"{(state.IsCursor ? "> " : "  ")}{Markup.Escape(s)}",
      AllowPin = true,
      InitialPinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a" },
    };

    var keys = Keys(K(ConsoleKey.Escape));
    var result = RunWith(picker, keys);

    Assert.Equal(ListPickerExitReason.Quit, result.Reason);
    Assert.Contains("a", result.Pinned);
  }

  [Fact]
  public void Resize_TriggersClearScreenInsteadOfClearLines()
  {
    var items = Enumerable.Range(0, 30).Select(i => $"item{i:00}").ToArray();
    var picker = MakePicker(items, multi: false);
    using var console = new TestTuiConsole(height: 20);

    var keys = new FakeKeyReader([]);
    keys.EnqueueIdle();
    keys.EnqueueAction(() => console.SetWindowHeight(10));
    keys.EnqueueIdle();
    keys.Enqueue(K(ConsoleKey.Enter));

    var result = picker.Show(console, keys);

    Assert.Equal(ListPickerExitReason.Confirmed, result.Reason);
    Assert.True(console.ClearScreenCallCount >= 1);
  }

  [Fact]
  public void TryReadKey_ReturnsFalse_ThenEnter_ConfirmsCleanly()
  {
    var items = new[] { "a", "b", "c" };
    var picker = MakePicker(items, multi: false);
    var keys = new FakeKeyReader([]);
    keys.EnqueueIdle();
    keys.Enqueue(K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(ListPickerExitReason.Confirmed, result.Reason);
    Assert.Equal(new[] { "a" }, result.Selected);
  }

  [Fact]
  public void Resize_BetweenKeystrokes_RedrawsWithNewMaxVisible()
  {
    var items = Enumerable.Range(0, 30).Select(i => $"item{i:00}").ToArray();
    var picker = MakePicker(items, multi: false);
    using var console = new TestTuiConsole(height: 20);

    var keys = new FakeKeyReader([]);
    keys.Enqueue(K(ConsoleKey.DownArrow));
    keys.EnqueueIdle();
    keys.EnqueueAction(() => console.SetWindowHeight(10));
    keys.Enqueue(K(ConsoleKey.Enter));

    var result = picker.Show(console, keys);

    Assert.Equal(ListPickerExitReason.Confirmed, result.Reason);
    Assert.Equal(new[] { "item01" }, result.Selected);

    // After resize to 10, MaxVisible = max(5, 10-6) = 5; the redraw triggered by the resize
    // produces a frame with only 5 visible items, so "more below" must appear.
    Assert.Contains("more below", console.Output, StringComparison.Ordinal);
  }

  [Fact]
  public void NoMultiSelect_Space_Ignored()
  {
    var items = new[] { "a", "b", "c" };
    var picker = MakePicker(items, multi: false);
    var keys = Keys(K(ConsoleKey.Spacebar, ' '), K(ConsoleKey.Spacebar, ' '), K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(ListPickerExitReason.Confirmed, result.Reason);
    Assert.Equal(new[] { "a" }, result.Selected);
    Assert.Empty(result.SelectedKeys ?? new HashSet<string>());
  }

  [Fact]
  public void AllowPinFalse_CtrlP_Ignored()
  {
    var items = new[] { "a", "b" };
    var picker = MakePicker(items, multi: false, allowPin: false);
    var keys = Keys(K(ConsoleKey.P, 'p', ctrl: true), K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Empty(result.Pinned);
  }

  [Fact]
  public void Filter_NarrowingViaCallback_ReceivesTypedText()
  {
    var items = new[] { "alpha", "beta", "gamma", "alphabet" };
    var receivedFilters = new List<string>();
    var picker = new ListPickerPrompt<string>
    {
      Header = "[bold]Pick[/]",
      Items = items,
      KeySelector = s => s,
      Renderer = (s, state) => $"{(state.IsCursor ? "> " : "  ")}{Markup.Escape(s)}",
      Filter = text =>
      {
        receivedFilters.Add(text);

        return string.IsNullOrEmpty(text)
          ? items
          : [.. items.Where(s => s.StartsWith(text, StringComparison.OrdinalIgnoreCase))];
      },
    };

    var keys = Keys(Char('b'), K(ConsoleKey.Enter));

    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "beta" }, result.Selected);
    Assert.Contains("b", receivedFilters);
  }

  [Fact]
  public void InitialCursorKey_PlacesCursorOnMatchingItem()
  {
    var items = new[] { "a", "b", "c", "d" };
    var picker = new ListPickerPrompt<string>
    {
      Header = "[bold]Pick[/]",
      Items = items,
      KeySelector = s => s,
      Renderer = (s, state) => $"{(state.IsCursor ? "> " : "  ")}{Markup.Escape(s)}",
      InitialCursorKey = "c",
    };

    var keys = Keys(K(ConsoleKey.Enter));
    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "c" }, result.Selected);
  }

  [Fact]
  public void InitialCursorKey_FallsBackToZero_WhenNotFound()
  {
    var items = new[] { "a", "b", "c" };
    var picker = new ListPickerPrompt<string>
    {
      Header = "[bold]Pick[/]",
      Items = items,
      KeySelector = s => s,
      Renderer = (s, state) => $"{(state.IsCursor ? "> " : "  ")}{Markup.Escape(s)}",
      InitialCursorKey = "missing",
    };

    var keys = Keys(K(ConsoleKey.Enter));
    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "a" }, result.Selected);
  }

  [Fact]
  public void DeleteHandler_RemovedResult_RebuildsListViaItemsSource()
  {
    var items = new List<string> { "a", "b", "c" };
    var picker = new ListPickerPrompt<string>
    {
      Header = "[bold]Pick[/]",
      Items = items,
      KeySelector = s => s,
      Renderer = (s, state) => $"{(state.IsCursor ? "> " : "  ")}{Markup.Escape(s)}",
      DeleteHandler = (item, _, _) =>
      {
        items.Remove(item);
        return ListPickerDeleteResult.Removed;
      },
      ItemsSource = items.ToArray,
    };

    // Cursor on "a"; Ctrl+D removes it; cursor now on "b"; Enter.
    var keys = Keys(K(ConsoleKey.D, 'd', ctrl: true), K(ConsoleKey.Enter));
    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "b" }, result.Selected);
    Assert.Equal(new[] { "b", "c" }, items.ToArray());
  }

  [Fact]
  public void DeleteHandler_NotApplicable_KeepsListUnchanged()
  {
    var items = new[] { "a", "b" };
    var picker = new ListPickerPrompt<string>
    {
      Header = "[bold]Pick[/]",
      Items = items,
      KeySelector = s => s,
      Renderer = (s, state) => $"{(state.IsCursor ? "> " : "  ")}{Markup.Escape(s)}",
      DeleteHandler = (_, _, _) => ListPickerDeleteResult.NotApplicable,
    };

    var keys = Keys(K(ConsoleKey.D, 'd', ctrl: true), K(ConsoleKey.Enter));
    var result = RunWith(picker, keys);

    Assert.Equal(new[] { "a" }, result.Selected);
  }

  private static ListPickerResult<string> RunWith(ListPickerPrompt<string> picker, FakeKeyReader keys)
  {
    using var console = new TestTuiConsole();

    return picker.Show(console, keys);
  }

  private static ListPickerPrompt<string> MakePicker(IReadOnlyList<string> items, bool multi, bool allowPin = false)
    => new()
    {
      Header = "[bold]Pick[/]",
      Items = items,
      KeySelector = s => s,
      Renderer = (s, state) => $"{(state.IsCursor ? "> " : "  ")}{Markup.Escape(s)}",
      MultiSelect = multi,
      AllowPin = allowPin,
    };

  private static ConsoleKeyInfo K(ConsoleKey key, char ch = '\0', bool ctrl = false)
    => new(ch, key, shift: false, alt: false, control: ctrl);

  private static ConsoleKeyInfo Char(char c)
  {
    var key = char.IsLetter(c)
      ? (ConsoleKey)char.ToUpperInvariant(c)
      : ConsoleKey.Oem1;

    return new ConsoleKeyInfo(c, key, shift: false, alt: false, control: false);
  }

  private static FakeKeyReader Keys(params ConsoleKeyInfo[] keys) => new(keys);
}
