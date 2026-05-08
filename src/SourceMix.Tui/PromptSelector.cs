using Hj.SourceMix.Core;
using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal static class PromptSelector
{
  private const string SkipKey = "__skip__";
  private const string NewKey = "__new__";

  private enum EntryKind
  {
    /// <summary>The "(none)" sentinel entry.</summary>
    Skip,

    /// <summary>A built-in prompt personality.</summary>
    BuiltIn,

    /// <summary>A user-defined custom prompt.</summary>
    Custom,

    /// <summary>The "+ New custom prompt" sentinel entry.</summary>
    New,
  }

  internal static PromptSelectorResult Show(
    GlobalPreferences globalPreferences,
    string? defaultPromptKey,
    ITuiConsole? tuiConsole = null,
    IKeyReader? keys = null)
  {
    ArgumentNullException.ThrowIfNull(globalPreferences);

    tuiConsole ??= new SystemTuiConsole();
    keys ??= new ConsoleKeyReader();

    var currentPrefs = globalPreferences;
    var entries = BuildEntries(currentPrefs);

    var picker = new ListPickerPrompt<PromptEntry>
    {
      Header = "[bold]Select a prompt personality[/]  [dim](Enter confirm \u00b7 Ctrl+D delete custom \u00b7 Ctrl+Q back)[/]",
      Items = entries,
      KeySelector = static e => e.Key,
      MultiSelect = false,
      AllowPin = false,
      Renderer = RenderRow,
      InitialCursorKey = defaultPromptKey,
      ItemsSource = () => BuildEntries(currentPrefs),
      DeleteHandler = (entry, console, keyReader) =>
      {
        if (entry.Kind != EntryKind.Custom)
        {
          return ListPickerDeleteResult.NotApplicable;
        }

        var (confirmed, quit) = ConfirmDelete(entry.Display, console, keyReader);

        if (quit || !confirmed)
        {
          return ListPickerDeleteResult.Cancelled;
        }

        var updatedPrompts = new Dictionary<string, string>(
          currentPrefs.CustomPrompts,
          StringComparer.OrdinalIgnoreCase);
        updatedPrompts.Remove(entry.Key);
        currentPrefs = currentPrefs with { CustomPrompts = updatedPrompts };

        return ListPickerDeleteResult.Removed;
      },
    };

    var result = picker.Show(tuiConsole, keys);

    if (result.Reason == ListPickerExitReason.Skipped)
    {
      return new PromptSelectorResult(null, null, currentPrefs, StepResult.Back);
    }

    if (result.Reason == ListPickerExitReason.Quit)
    {
      return new PromptSelectorResult(null, null, currentPrefs, StepResult.Quit);
    }

    if (result.Selected.Count == 0)
    {
      return new PromptSelectorResult(null, null, currentPrefs, StepResult.Back);
    }

    var entry = result.Selected[0];

    if (entry.Key == SkipKey)
    {
      return new PromptSelectorResult(null, null, currentPrefs, StepResult.Confirm);
    }

    if (entry.Key == NewKey)
    {
      return CreateCustomPrompt(currentPrefs, defaultPromptKey, tuiConsole, keys);
    }

    return new PromptSelectorResult(entry.Text, entry.Key, currentPrefs, StepResult.Confirm);
  }

  private static (bool Confirmed, bool Quit) ConfirmDelete(string displayName, ITuiConsole console, IKeyReader keys)
  {
    var (text, ok, quit) = TextInputPrompt.Read(
      $"Delete custom prompt '{displayName}'? Type 'y' to confirm:",
      string.Empty,
      console,
      keys);

    if (quit)
    {
      return (false, true);
    }

    if (!ok)
    {
      return (false, false);
    }

    return (string.Equals(text.Trim(), "y", StringComparison.OrdinalIgnoreCase), false);
  }

  private static List<PromptEntry> BuildEntries(GlobalPreferences globalPreferences)
  {
    var allEntries = new List<PromptEntry>
    {
      new(SkipKey, "(none — skip prompt)", string.Empty, EntryKind.Skip),
    };

    foreach (var (key, prompt) in BuiltInPrompts.All)
    {
      allEntries.Add(new PromptEntry(key, prompt.DisplayName, prompt.Text, EntryKind.BuiltIn));
    }

    foreach (var (name, text) in globalPreferences.CustomPrompts)
    {
      allEntries.Add(new PromptEntry(name, name, text, EntryKind.Custom));
    }

    allEntries.Add(new PromptEntry(NewKey, "+ New custom prompt", string.Empty, EntryKind.New));

    return allEntries;
  }

  private static string RenderRow(PromptEntry entry, ListPickerItemState state)
  {
    var arrow = state.IsCursor ? "[darkorange]>[/]" : " ";
    var label = state.IsCursor
      ? $"[bold]{Markup.Escape(entry.Display)}[/]"
      : $"[dim]{Markup.Escape(entry.Display)}[/]";
    var suffix = entry.Kind switch
    {
      EntryKind.BuiltIn => $"  [dim][[{Markup.Escape(entry.Key)}]][/]",
      EntryKind.Custom => "  [dim][[custom]][/]",
      _ => string.Empty,
    };

    return $" {arrow} {label}{suffix}";
  }

  private static PromptSelectorResult CreateCustomPrompt(
    GlobalPreferences globalPreferences,
    string? defaultPromptKey,
    ITuiConsole console,
    IKeyReader keys)
  {
    while (true)
    {
      var (name, nameOk, nameQuit) = TextInputPrompt.Read(
        "Prompt name:",
        string.Empty,
        console,
        keys,
        static n => string.IsNullOrWhiteSpace(n) ? "Name cannot be empty." : null);

      if (nameQuit)
      {
        return new PromptSelectorResult(null, null, globalPreferences, StepResult.Quit);
      }

      if (!nameOk)
      {
        return Show(globalPreferences, defaultPromptKey, console, keys);
      }

      var (text, textOk, textQuit) = TextInputPrompt.Read(
        "Prompt text:",
        string.Empty,
        console,
        keys,
        static t => string.IsNullOrWhiteSpace(t) ? "Prompt text cannot be empty." : null);

      if (textQuit)
      {
        return new PromptSelectorResult(null, null, globalPreferences, StepResult.Quit);
      }

      if (!textOk)
      {
        continue;
      }

      var trimmedName = name.Trim();
      var trimmedText = text.Trim();

      var updatedPrompts = new Dictionary<string, string>(
        globalPreferences.CustomPrompts,
        StringComparer.OrdinalIgnoreCase)
      {
        [trimmedName] = trimmedText,
      };

      var updatedPreferences = globalPreferences with { CustomPrompts = updatedPrompts };

      return Show(updatedPreferences, defaultPromptKey, console, keys);
    }
  }

  private sealed record PromptEntry(string Key, string Display, string Text, EntryKind Kind);
}
