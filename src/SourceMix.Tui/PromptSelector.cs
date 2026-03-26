using Hj.SourceMix.Core;
using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal static class PromptSelector
{
  private const string SkipLabel = "(none — skip prompt)";
  private const string NewLabel = "+ New custom prompt";

  internal static (string? PromptText, string? PromptKey, GlobalPreferences Preferences) Show(
    GlobalPreferences globalPreferences,
    string? defaultPromptKey)
  {
    AnsiConsole.MarkupLine("[bold]Select a prompt personality[/] [dim](optional)[/]");
    AnsiConsole.WriteLine();

    var (choices, labelToKey) = BuildChoices(globalPreferences, defaultPromptKey);

    var selection = AnsiConsole.Prompt(
      new SelectionPrompt<string>()
        .PageSize(12)
        .HighlightStyle(new Style(Color.DarkOrange))
        .AddChoices([.. choices.Keys]));

    if (selection == SkipLabel)
    {
      return (null, null, globalPreferences);
    }

    if (selection == NewLabel)
    {
      return CreateCustomPrompt(globalPreferences);
    }

    return (choices[selection], labelToKey[selection], globalPreferences);
  }

  private static (Dictionary<string, string> Choices, Dictionary<string, string> LabelToKey) BuildChoices(
    GlobalPreferences globalPreferences,
    string? defaultPromptKey)
  {
    var allEntries = new List<(string Label, string Text, string Key)>();

    foreach (var (key, prompt) in BuiltInPrompts.All)
    {
      allEntries.Add(($"{prompt.DisplayName}  [[{key}]]", prompt.Text, key));
    }

    foreach (var (name, text) in globalPreferences.CustomPrompts)
    {
      allEntries.Add(($"{name}  [[custom]]", text, name));
    }

    if (defaultPromptKey is not null)
    {
      var defaultIndex = allEntries.FindIndex(e =>
        string.Equals(e.Key, defaultPromptKey, StringComparison.OrdinalIgnoreCase));

      if (defaultIndex > 0)
      {
        var defaultEntry = allEntries[defaultIndex];
        allEntries.RemoveAt(defaultIndex);
        allEntries.Insert(0, defaultEntry);
      }
    }

    var choices = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      [SkipLabel] = string.Empty,
    };

    var labelToKey = new Dictionary<string, string>(StringComparer.Ordinal);

    foreach (var (label, text, key) in allEntries)
    {
      choices[label] = text;
      labelToKey[label] = key;
    }

    choices[NewLabel] = string.Empty;

    return (choices, labelToKey);
  }

  private static (string? PromptText, string? PromptKey, GlobalPreferences Preferences) CreateCustomPrompt(
    GlobalPreferences globalPreferences)
  {
    AnsiConsole.WriteLine();

    var name = AnsiConsole.Prompt(
      new TextPrompt<string>("Prompt name:")
        .Validate(static n => !string.IsNullOrWhiteSpace(n)
          ? ValidationResult.Success()
          : ValidationResult.Error("[red]Name cannot be empty.[/]")));

    var text = AnsiConsole.Prompt(
      new TextPrompt<string>("Prompt text:")
        .Validate(static t => !string.IsNullOrWhiteSpace(t)
          ? ValidationResult.Success()
          : ValidationResult.Error("[red]Prompt text cannot be empty.[/]")));

    var updatedPrompts = new Dictionary<string, string>(
      globalPreferences.CustomPrompts,
      StringComparer.OrdinalIgnoreCase)
    {
      [name.Trim()] = text.Trim(),
    };

    var updatedPreferences = globalPreferences with { CustomPrompts = updatedPrompts };

    return (text.Trim(), name.Trim(), updatedPreferences);
  }
}
