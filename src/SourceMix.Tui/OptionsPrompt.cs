using Hj.SourceMix.Core;
using Spectre.Console;

namespace Hj.SourceMix.Tui;

internal sealed record MixOptions(
  bool Recursive,
  int MaxDepth,
  bool IncludeCompiled,
  string OutputPath);

internal static class OptionsPrompt
{
  internal static MixOptions Show(SolutionPreferences preferences)
  {
    AnsiConsole.MarkupLine("[bold]Configure options[/]");
    AnsiConsole.WriteLine();

    var defaults = preferences.Defaults;

    var recursive = AnsiConsole.Confirm("Include [blue]recursive[/] type dependencies?", defaultValue: defaults.Recursive);

    var maxDepth = int.MaxValue;
    var includeCompiled = false;

    if (recursive)
    {
      var limitDepth = AnsiConsole.Confirm("  Limit recursion [blue]depth[/]?", defaultValue: defaults.LimitDepth);

      if (limitDepth)
      {
        maxDepth = AnsiConsole.Prompt(
          new TextPrompt<int>("  Max depth:")
            .DefaultValue(defaults.MaxDepth)
            .Validate(static d => d > 0
              ? ValidationResult.Success()
              : ValidationResult.Error("[red]Depth must be greater than 0.[/]")));
      }

      includeCompiled = AnsiConsole.Confirm(
        "  [blue]Decompile[/] interfaces and models from compiled assemblies ([dim]requires dotnet build[/])?",
        defaultValue: defaults.IncludeCompiled);
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

    return new MixOptions(recursive, maxDepth, includeCompiled, outputPath);
  }
}
