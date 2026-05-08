using System.IO.Abstractions;
using Hj.SourceMix.Core;

namespace Hj.SourceMix;

internal static class SourceMixCli
{
  public static async Task<int> RunAsync(
    CliRequest request,
    IFileSystem? fileSystem = null,
    string? baseDirectory = null,
    TextWriter? standardOutput = null,
    TextWriter? standardError = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var fileSystemToUse = fileSystem ?? new FileSystem();
    var currentDirectory = baseDirectory ?? Directory.GetCurrentDirectory();
    var outputWriter = standardOutput ?? Console.Out;
    var errorWriter = standardError ?? Console.Error;

    if (!ValidateRequest(request, fileSystemToUse, errorWriter))
    {
      return 1;
    }

    var seedFiles = GlobResolver.Resolve(fileSystemToUse, request.Files, currentDirectory);
    var seedPathSet = new HashSet<string>(seedFiles, StringComparer.OrdinalIgnoreCase);
    var resolvedFiles = (IReadOnlyList<string>)seedFiles;
    IReadOnlySet<string> unresolvedTypeNames = new HashSet<string>();

    if (request.Recursive && seedFiles.Count > 0)
    {
      var solutionDirectory = SolutionFinder.FindSolutionDirectory(fileSystemToUse, currentDirectory);

      if (solutionDirectory is not null)
      {
        var result = DependencyResolver.Resolve(fileSystemToUse, seedFiles, solutionDirectory, request.Depth);
        resolvedFiles = result.Files;
        unresolvedTypeNames = result.UnresolvedTypeNames;
      }
    }

    var sourceFiles = new List<SourceFile>(resolvedFiles.Count);

    foreach (var filePath in resolvedFiles)
    {
      var sourceText = await fileSystemToUse.File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
      var isSeed = seedPathSet.Contains(filePath);
      sourceFiles.Add(new SourceFile(filePath, sourceText, isSeed));
    }

    var processedSources = SourceProcessor.ProcessBatch(sourceFiles, request.Trim, request.ExpandTypes);

    var decompiledSources = new List<string>();

    if (request.IncludeCompiled && request.Recursive && unresolvedTypeNames.Count > 0)
    {
      var solutionDirectory = SolutionFinder.FindSolutionDirectory(fileSystemToUse, currentDirectory);

      if (solutionDirectory is not null)
      {
        var decompiled = AssemblyDecompiler.Decompile(unresolvedTypeNames, solutionDirectory);

        foreach (var source in decompiled)
        {
          decompiledSources.Add(SourceProcessor.Process(source, trim: request.Trim));
        }
      }
    }

    var skillContents = new List<string>();

    if (request.SkillKeys.Length > 0)
    {
      var skillsDirectory = PreferencesManager.GetSkillsDirectory();
      var allSkills = SkillScanner.GetAllSkills(fileSystemToUse, skillsDirectory);
      var skillMap = allSkills.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

      foreach (var key in request.SkillKeys)
      {
        if (skillMap.TryGetValue(key, out var skill))
        {
          skillContents.Add(await fileSystemToUse.File.ReadAllTextAsync(skill.FilePath, cancellationToken).ConfigureAwait(false));
        }
        else
        {
          errorWriter.WriteLine($"Warning: skill '{key}' not found in {skillsDirectory}");
        }
      }
    }

    if (request.Output is null)
    {
      if (skillContents.Count > 0)
      {
        OutputFormatter.WriteSkills(skillContents, outputWriter);
      }

      OutputFormatter.WriteCode(processedSources, decompiledSources, outputWriter);
      WritePromptIfSet(request.Prompt, outputWriter);
      return 0;
    }

    try
    {
      await using var stream = fileSystemToUse.FileInfo
        .New(request.Output.FullName)
        .Open(FileMode.Create, FileAccess.Write, FileShare.None);
      await using var writer = new StreamWriter(stream);

      if (skillContents.Count > 0)
      {
        OutputFormatter.WriteSkills(skillContents, writer);
      }

      OutputFormatter.WriteCode(processedSources, decompiledSources, writer);
      WritePromptIfSet(request.Prompt, writer);
    }
    catch (DirectoryNotFoundException)
    {
      var outputDirectory = Path.GetDirectoryName(request.Output.FullName) ?? request.Output.DirectoryName ?? request.Output.FullName;
      errorWriter.WriteLine($"Error: output directory does not exist: {outputDirectory}");
      return 1;
    }
    catch (UnauthorizedAccessException)
    {
      errorWriter.WriteLine($"Error: unable to write to output path '{request.Output.FullName}'.");
      return 1;
    }

    return 0;
  }

  private static bool ValidateRequest(CliRequest request, IFileSystem fileSystem, TextWriter errorWriter)
  {
    var isValid = true;

    if (request.Trim && !request.Recursive)
    {
      errorWriter.WriteLine("Error: --trim requires --recursive.");
      isValid = false;
    }

    if (request.IncludeCompiled && !request.Recursive)
    {
      errorWriter.WriteLine("Error: --include-compiled requires --recursive.");
      isValid = false;
    }

    if (request.Output is not null)
    {
      var outputDirectory = Path.GetDirectoryName(request.Output.FullName);

      if (!string.IsNullOrEmpty(outputDirectory) && !fileSystem.Directory.Exists(outputDirectory))
      {
        errorWriter.WriteLine($"Error: output directory does not exist: {outputDirectory}");
        isValid = false;
      }
    }

    return isValid;
  }

  private static void WritePromptIfSet(string? promptValue, TextWriter writer)
  {
    if (promptValue is null)
    {
      return;
    }

    if (BuiltInPrompts.All.TryGetValue(promptValue, out var builtIn))
    {
      OutputFormatter.WritePrompt(writer, builtIn.Text);
      return;
    }

    OutputFormatter.WritePrompt(writer, promptValue);
  }
}
