using Hj.SourceMix.Core;
using Hj.SourceMix.Tui;
using Spectre.Console;
using System.IO.Abstractions;

var fileSystem = new FileSystem();
var baseDirectory = Directory.GetCurrentDirectory();
var solutionDirectory = SolutionFinder.FindSolutionDirectory(fileSystem, baseDirectory);

if (solutionDirectory is null)
{
  AnsiConsole.MarkupLine("[red]Error:[/] No solution file (.sln or .slnx) found in the current directory or any parent.");
  return 1;
}

AnsiConsole.MarkupLine($"[bold]SourceMix[/] [dim]— {Markup.Escape(solutionDirectory)}[/]");
AnsiConsole.WriteLine();

var preferences = PreferencesManager.Load(fileSystem, solutionDirectory);
var globalPreferences = PreferencesManager.LoadGlobal(fileSystem);

var scanner = new CsFileScanner(fileSystem, solutionDirectory);
IReadOnlyList<CsFile> allFiles = [];

AnsiConsole.Status()
  .Start("Scanning for .cs files...", ctx =>
  {
    allFiles = scanner.GetAllFiles();
  });

if (allFiles.Count == 0)
{
  AnsiConsole.MarkupLine("[yellow]No .cs files found in the solution directory.[/]");
  return 0;
}

AnsiConsole.MarkupLine($"[dim]Found {allFiles.Count} .cs files.[/]");
AnsiConsole.WriteLine();

var pinnedFullPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

foreach (var relativePath in preferences.PinnedFiles)
{
  var fullPath = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(solutionDirectory, relativePath));

  if (allFiles.Any(f => f.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase)))
  {
    pinnedFullPaths.Add(fullPath);
  }
}

var selection = FileSearchPrompt.Show(allFiles, pinnedFullPaths);

if (selection.SelectedPaths.Count == 0)
{
  AnsiConsole.MarkupLine("[yellow]No files selected. Exiting.[/]");
  return 0;
}

var selectedPaths = selection.SelectedPaths;
var seedPathSet = new HashSet<string>(selectedPaths, StringComparer.OrdinalIgnoreCase);

AnsiConsole.WriteLine();
var options = OptionsPrompt.Show(preferences);
AnsiConsole.WriteLine();

var (selectedPromptText, updatedGlobalPreferences) = PromptSelector.Show(globalPreferences);
AnsiConsole.WriteLine();

IReadOnlyList<string> resolvedFiles = selectedPaths;
IReadOnlySet<string> unresolvedTypeNames = new HashSet<string>();

if (options.Recursive)
{
  await AnsiConsole.Status()
    .StartAsync("Resolving dependencies...", async ctx =>
    {
      await Task.Run(() =>
      {
        var result = DependencyResolver.Resolve(fileSystem, selectedPaths, solutionDirectory, options.MaxDepth);
        resolvedFiles = result.Files;
        unresolvedTypeNames = result.UnresolvedTypeNames;
      });
    });

  AnsiConsole.MarkupLine($"[dim]Resolved {resolvedFiles.Count} files ({resolvedFiles.Count - selectedPaths.Count} dependencies added).[/]");
}

var processedSources = new List<string>(resolvedFiles.Count);

await AnsiConsole.Progress()
  .StartAsync(async ctx =>
  {
    var task = ctx.AddTask("[green]Processing source files[/]", maxValue: resolvedFiles.Count);

    foreach (var filePath in resolvedFiles)
    {
      var sourceText = await fileSystem.File.ReadAllTextAsync(filePath);
      var isSeed = seedPathSet.Contains(filePath);
      processedSources.Add(SourceProcessor.Process(sourceText, trim: options.Trim && !isSeed));
      task.Increment(1);
    }
  });

var decompiledSources = new List<string>();

if (options.IncludeCompiled && unresolvedTypeNames.Count > 0)
{
  await AnsiConsole.Status()
    .StartAsync("Decompiling referenced types from assemblies...", async ctx =>
    {
      await Task.Run(() =>
      {
        var decompiled = AssemblyDecompiler.Decompile(unresolvedTypeNames, solutionDirectory);

        foreach (var source in decompiled)
        {
          decompiledSources.Add(SourceProcessor.Process(source));
        }
      });
    });

  if (decompiledSources.Count > 0)
  {
    AnsiConsole.MarkupLine($"[dim]Decompiled {decompiledSources.Count} types from assemblies.[/]");
  }
}

var outputFile = new FileInfo(options.OutputPath);

await using (var stream = outputFile.Open(FileMode.Create, FileAccess.Write, FileShare.None))
await using (var writer = new StreamWriter(stream))
{
  OutputFormatter.Write(processedSources, decompiledSources, writer);

  if (selectedPromptText is not null)
  {
    OutputFormatter.WritePrompt(writer, selectedPromptText);
  }
}

var pinnedRelativePaths = selection.PinnedPaths
  .Select(p => fileSystem.Path.GetRelativePath(solutionDirectory, p))
  .Order(StringComparer.OrdinalIgnoreCase)
  .ToList();

var updatedPreferences = new SolutionPreferences
{
  SolutionPath = solutionDirectory,
  PinnedFiles = pinnedRelativePaths,
  OutputPath = options.OutputPath,
  Defaults = new PreferenceDefaults
  {
    Recursive = options.Recursive,
    LimitDepth = options.MaxDepth != int.MaxValue,
    MaxDepth = options.MaxDepth == int.MaxValue ? 3 : options.MaxDepth,
    IncludeCompiled = options.IncludeCompiled,
    Trim = options.Trim,
  },
};

PreferencesManager.Save(fileSystem, solutionDirectory, updatedPreferences);

if (!ReferenceEquals(globalPreferences, updatedGlobalPreferences))
{
  PreferencesManager.SaveGlobal(fileSystem, updatedGlobalPreferences);
}

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine($"[green]✓[/] Written to [bold]{Markup.Escape(options.OutputPath)}[/]");
AnsiConsole.MarkupLine($"  [dim]{processedSources.Count} source file(s)[/]{(decompiledSources.Count > 0 ? $"[dim], {decompiledSources.Count} decompiled type(s)[/]" : string.Empty)}");

return 0;

