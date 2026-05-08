using System.IO.Abstractions;
using Hj.SourceMix.Core;
using Spectre.Console;

namespace Hj.SourceMix.Tui;

public static class SourceMixTuiApplication
{
  public static async Task<int> RunAsync(CancellationToken cancellationToken = default)
  {
    return await RunAsync(new FileSystem(), Directory.GetCurrentDirectory(), cancellationToken).ConfigureAwait(false);
  }

  internal static async Task<int> RunAsync(
    IFileSystem fileSystem,
    string baseDirectory,
    CancellationToken cancellationToken = default)
  {
    return await RunAsync(
      fileSystem,
      baseDirectory,
      AnsiConsole.Console,
      new SystemTuiConsole(),
      new ConsoleKeyReader(),
      cancellationToken).ConfigureAwait(false);
  }

  internal static async Task<int> RunAsync(
    IFileSystem fileSystem,
    string baseDirectory,
    IAnsiConsole ansiConsole,
    ITuiConsole tuiConsole,
    IKeyReader keyReader,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(fileSystem);
    ArgumentNullException.ThrowIfNull(baseDirectory);
    ArgumentNullException.ThrowIfNull(ansiConsole);
    ArgumentNullException.ThrowIfNull(tuiConsole);
    ArgumentNullException.ThrowIfNull(keyReader);

    var solutionDirectory = SolutionFinder.FindSolutionDirectory(fileSystem, baseDirectory);

    if (solutionDirectory is null)
    {
      ansiConsole.MarkupLine("[red]Error:[/] No solution file (.sln or .slnx) found in the current directory or any parent.");
      return 1;
    }

    ansiConsole.MarkupLine($"[bold]SourceMix[/] [dim]— {Markup.Escape(solutionDirectory)}[/]");
    ansiConsole.WriteLine();

    var preferences = PreferencesManager.Load(fileSystem, solutionDirectory);
    var globalPreferences = PreferencesManager.LoadGlobal(fileSystem);

    var scanner = new CsFileScanner(fileSystem, solutionDirectory);
    IReadOnlyList<CsFile> allFiles = [];

    ansiConsole.Status()
      .Start("Scanning for .cs files...", _ =>
      {
        allFiles = scanner.GetAllFiles();
      });

    if (allFiles.Count == 0)
    {
      ansiConsole.MarkupLine("[yellow]No .cs files found in the solution directory.[/]");
      return 0;
    }

    ansiConsole.MarkupLine($"[dim]Found {allFiles.Count} .cs files.[/]");
    ansiConsole.WriteLine();

    var pinnedFullPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var relativePath in preferences.PinnedFiles)
    {
      var fullPath = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(solutionDirectory, relativePath));

      if (allFiles.Any(f => f.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase)))
      {
        pinnedFullPaths.Add(fullPath);
      }
    }

    var skillsDirectory = PreferencesManager.GetSkillsDirectory();
    var allSkills = SkillScanner.GetAllSkills(fileSystem, skillsDirectory);
    var pinnedSkillKeys = new HashSet<string>(preferences.PinnedSkills, StringComparer.OrdinalIgnoreCase);

    var wizard = TuiWizard.Run(
      globalPreferences,
      preferences,
      allFiles,
      pinnedFullPaths,
      allSkills,
      pinnedSkillKeys,
      tuiConsole,
      keyReader,
      fileSystem);

    if (wizard.Quit)
    {
      ansiConsole.MarkupLine("[yellow]Cancelled.[/]");
      return 0;
    }

    if (!wizard.Confirmed)
    {
      ansiConsole.MarkupLine("[yellow]Cancelled.[/]");
      return 0;
    }

    var options = wizard.Options!;
    var selectedPaths = wizard.SelectedFiles;
    var seedPathSet = new HashSet<string>(selectedPaths, StringComparer.OrdinalIgnoreCase);

    var resolvedFiles = selectedPaths;
    IReadOnlySet<string> unresolvedTypeNames = new HashSet<string>();

    if (options.Recursive)
    {
      await ansiConsole.Status()
        .StartAsync("Resolving dependencies...", async _ =>
        {
          await Task.Run(
            () =>
            {
              var result = DependencyResolver.Resolve(fileSystem, selectedPaths, solutionDirectory, options.MaxDepth);
              resolvedFiles = result.Files;
              unresolvedTypeNames = result.UnresolvedTypeNames;
            },
            cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

      ansiConsole.MarkupLine($"[dim]Resolved {resolvedFiles.Count} files ({resolvedFiles.Count - selectedPaths.Count} dependencies added).[/]");
    }

    var sourceFiles = new List<SourceFile>(resolvedFiles.Count);

    await ansiConsole.Progress()
      .StartAsync(async ctx =>
      {
        var task = ctx.AddTask("[green]Reading source files[/]", maxValue: resolvedFiles.Count);

        foreach (var filePath in resolvedFiles)
        {
          var sourceText = await fileSystem.File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
          var isSeed = seedPathSet.Contains(filePath);
          sourceFiles.Add(new SourceFile(filePath, sourceText, isSeed));
          task.Increment(1);
        }
      }).ConfigureAwait(false);

    var processedSources = new List<string>(resolvedFiles.Count);

    await ansiConsole.Status()
      .StartAsync("Processing source files...", async _ =>
      {
        var processed = await Task.Run(
          () => SourceProcessor.ProcessBatch(sourceFiles, options.Trim, options.ExpandTypes),
          cancellationToken).ConfigureAwait(false);
        processedSources.AddRange(processed);
      }).ConfigureAwait(false);

    var decompiledSources = new List<string>();

    if (options.IncludeCompiled && unresolvedTypeNames.Count > 0)
    {
      await ansiConsole.Status()
        .StartAsync("Decompiling referenced types from assemblies...", async _ =>
        {
          await Task.Run(
            () =>
            {
              var decompiled = AssemblyDecompiler.Decompile(unresolvedTypeNames, solutionDirectory);

              foreach (var source in decompiled)
              {
                decompiledSources.Add(SourceProcessor.Process(source, trim: options.Trim));
              }
            },
            cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

      if (decompiledSources.Count > 0)
      {
        ansiConsole.MarkupLine($"[dim]Decompiled {decompiledSources.Count} types from assemblies.[/]");
      }
    }

    var fileMode = FileMode.Create;
    if (fileSystem.File.Exists(options.OutputPath))
    {
      var choice = ansiConsole.Prompt(new SelectionPrompt<string>()
        .Title($"Output file [bold]{Markup.Escape(options.OutputPath)}[/] exists. What do you want to do?")
        .AddChoices("Overwrite", "Append", "Cancel"));
      switch (choice)
      {
        case "Append":
          fileMode = FileMode.Append;
          break;
        case "Cancel":
          ansiConsole.MarkupLine("[yellow]Cancelled.[/]");
          return 0;
      }
    }

    var skillContents = wizard.SelectedSkillKeys
      .Select(k => allSkills.FirstOrDefault(s => string.Equals(s.Key, k, StringComparison.OrdinalIgnoreCase)))
      .Where(s => s is not null)
      .Select(s => fileSystem.File.ReadAllText(s!.FilePath))
      .ToList();

    await using (var stream = fileSystem.File.Open(options.OutputPath, fileMode, FileAccess.Write, FileShare.None))
    await using (var writer = new StreamWriter(stream))
    {
      if (skillContents.Count > 0)
      {
        OutputFormatter.WriteSkills(skillContents, writer);
      }

      OutputFormatter.WriteCode(processedSources, decompiledSources, writer);

      if (wizard.PromptText is not null)
      {
        OutputFormatter.WritePrompt(writer, wizard.PromptText);
      }
    }

    var pinnedRelativePaths = wizard.PinnedFiles
      .Select(p => fileSystem.Path.GetRelativePath(solutionDirectory, p))
      .Order(StringComparer.OrdinalIgnoreCase)
      .ToList();

    var updatedPreferences = new SolutionPreferences
    {
      SolutionPath = solutionDirectory,
      PinnedFiles = pinnedRelativePaths,
      OutputPath = options.OutputPath,
      DefaultPromptKey = wizard.PromptKey,
      PinnedSkills = [.. wizard.PinnedSkillKeys.Order(StringComparer.OrdinalIgnoreCase)],
      Defaults = new PreferenceDefaults
      {
        Recursive = options.Recursive,
        LimitDepth = options.MaxDepth != int.MaxValue,
        MaxDepth = options.MaxDepth == int.MaxValue ? 3 : options.MaxDepth,
        IncludeCompiled = options.IncludeCompiled,
        Trim = options.Trim,
        ExpandTypes = options.ExpandTypes,
      },
    };

    PreferencesManager.Save(fileSystem, solutionDirectory, updatedPreferences);

    if (!ReferenceEquals(globalPreferences, wizard.GlobalPreferences))
    {
      PreferencesManager.SaveGlobal(fileSystem, wizard.GlobalPreferences);
    }

    ansiConsole.WriteLine();
    ansiConsole.MarkupLine($"[green]✓[/] Written to [bold]{Markup.Escape(options.OutputPath)}[/]");
    ansiConsole.MarkupLine($"  [dim]{processedSources.Count} source file(s)[/]{(decompiledSources.Count > 0 ? $"[dim], {decompiledSources.Count} decompiled type(s)[/]" : string.Empty)}");

    return 0;
  }
}
