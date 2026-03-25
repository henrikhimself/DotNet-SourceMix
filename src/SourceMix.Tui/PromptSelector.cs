using Hj.SourceMix.Core;
using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal static class PromptSelector
{
  private const string SkipLabel = "(none — skip prompt)";
  private const string NewLabel = "+ New custom prompt";

  internal static (string? PromptText, GlobalPreferences Preferences) Show(GlobalPreferences globalPreferences)
  {
    AnsiConsole.MarkupLine("[bold]Select a prompt personality[/] [dim](optional)[/]");
    AnsiConsole.WriteLine();

    var choices = BuildChoices(globalPreferences);

    var selection = AnsiConsole.Prompt(
      new SelectionPrompt<string>()
        .PageSize(12)
        .AddChoices([.. choices.Keys]));

    if (selection == SkipLabel)
    {
      return (null, globalPreferences);
    }

    if (selection == NewLabel)
    {
      return CreateCustomPrompt(globalPreferences);
    }

    return (choices[selection], globalPreferences);
  }

  private static Dictionary<string, string> BuildChoices(GlobalPreferences globalPreferences)
  {
    var choices = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      [SkipLabel] = string.Empty,
    };

    foreach (var (key, prompt) in BuiltInPrompts.All)
    {
      choices[$"{prompt.DisplayName}  [[{key}]]"] = prompt.Text;
    }

    foreach (var (name, text) in globalPreferences.CustomPrompts)
    {
      choices[$"{name}  [[custom]]"] = text;
    }

    choices[NewLabel] = string.Empty;

    return choices;
  }

  private static (string? PromptText, GlobalPreferences Preferences) CreateCustomPrompt(
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

    return (text.Trim(), updatedPreferences);
  }
}
