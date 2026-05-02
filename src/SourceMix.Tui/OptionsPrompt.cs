using Hj.SourceMix.Core;
using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal static class OptionsPrompt
{
  internal static MixOptions Show(SolutionPreferences preferences)
  {
    AnsiConsole.MarkupLine("[bold]Configure options[/]");
    AnsiConsole.WriteLine();

    var defaults = preferences.Defaults;

    var recursive = new ConfirmationPrompt("Include [darkorange]recursive[/] type dependencies?")
    {
      DefaultValue = defaults.Recursive,
    }
    .ChoicesStyle(new Style(Color.DarkOrange))
    .Show(AnsiConsole.Console);

    var maxDepth = int.MaxValue;
    var includeCompiled = false;
    var trim = false;

    if (recursive)
    {
      var limitDepth = new ConfirmationPrompt("  Limit recursion [darkorange]depth[/]?")
      {
        DefaultValue = defaults.LimitDepth,
      }
      .ChoicesStyle(new Style(Color.DarkOrange))
      .Show(AnsiConsole.Console);

      if (limitDepth)
      {
        maxDepth = AnsiConsole.Prompt(
          new TextPrompt<int>("  Max depth:")
            .DefaultValue(defaults.MaxDepth)
            .Validate(static d => d > 0
              ? ValidationResult.Success()
              : ValidationResult.Error("[red]Depth must be greater than 0.[/]")));
      }

      includeCompiled = new ConfirmationPrompt(
        "  [darkorange]Decompile[/] interfaces and models from compiled assemblies ([dim]requires dotnet build[/])?")
      {
        DefaultValue = defaults.IncludeCompiled,
      }
      .ChoicesStyle(new Style(Color.DarkOrange))
      .Show(AnsiConsole.Console);

      trim = new ConfirmationPrompt(
        "  [darkorange]Trim[/] method bodies from dependency files ([dim]keep signatures only[/])?")
      {
        DefaultValue = defaults.Trim,
      }
      .ChoicesStyle(new Style(Color.DarkOrange))
      .Show(AnsiConsole.Console);
    }

    var defaultOutput = preferences.OutputPath
      ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "sourcemix.md");

    var outputPath = AnsiConsole.Prompt(
      new TextPrompt<string>("Output file:")
        .DefaultValue(defaultOutput)
        .Validate(static path =>
        {
          var dir = Path.GetDirectoryName(path);

          return string.IsNullOrEmpty(dir) || Directory.Exists(dir)
            ? ValidationResult.Success()
            : ValidationResult.Error($"[red]Directory does not exist: {Markup.Escape(dir)}[/]");
        }));

    return new MixOptions(recursive, maxDepth, includeCompiled, trim, outputPath);
  }
}
