using System.CommandLine;
using System.IO.Abstractions;
using Hj.SourceMix.Core;

var filesArgument = new Argument<string[]>("files")
{
  Description = "File paths or glob patterns of C# files to include (e.g. src/**/*.cs).",
  Arity = ArgumentArity.ZeroOrMore,
};

var outputOption = new Option<FileInfo?>("--output", "-o")
{
  Description = "Output file path. Defaults to stdout when not specified.",
};

var recursiveOption = new Option<bool>("--recursive", "-r")
{
  Description = "Recursively include files that define types referenced by the specified files.",
};

var depthOption = new Option<int>("--depth", "-d")
{
  Description = "Maximum recursion depth when --recursive is used. Defaults to no limit.",
  DefaultValueFactory = _ => int.MaxValue,
};

var includeCompiledOption = new Option<bool>("--include-compiled", "-c")
{
  Description = "Decompile interfaces and simple model types from compiled assemblies in bin/ for types not found in source. Requires --recursive and a prior dotnet build.",
};

var trimOption = new Option<bool>("--trim", "-t")
{
  Description = "Strip method bodies from dependency files, keeping type signatures only. Requires --recursive.",
};

var promptOption = new Option<string?>("--prompt", "-p")
{
  Description = "Append a prompt personality to the output. Use a built-in key (unit-test, code-review, tech-docs, explain, debug, refactor, architecture) or provide custom text.",
};

var rootCommand = new RootCommand("Collects .NET C# source files into an LLM AI optimized Markdown file.")
{
  filesArgument,
  outputOption,
  recursiveOption,
  depthOption,
  includeCompiledOption,
  trimOption,
  promptOption,
};

rootCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
{
  var fileSystem = new FileSystem();
  var files = parseResult.GetValue(filesArgument) ?? [];
  var output = parseResult.GetValue(outputOption);
  var recursive = parseResult.GetValue(recursiveOption);
  var depth = parseResult.GetValue(depthOption);
  var includeCompiled = parseResult.GetValue(includeCompiledOption);
  var trim = parseResult.GetValue(trimOption);
  var promptValue = parseResult.GetValue(promptOption);

  var baseDirectory = Directory.GetCurrentDirectory();
  var seedFiles = GlobResolver.Resolve(fileSystem, files, baseDirectory);
  var seedPathSet = new HashSet<string>(seedFiles, StringComparer.OrdinalIgnoreCase);
  var resolvedFiles = (IReadOnlyList<string>)seedFiles;

  IReadOnlySet<string> unresolvedTypeNames = new HashSet<string>();

  if (recursive && seedFiles.Count > 0)
  {
    var solutionDirectory = SolutionFinder.FindSolutionDirectory(fileSystem, baseDirectory);

    if (solutionDirectory is not null)
    {
      var result = DependencyResolver.Resolve(fileSystem, seedFiles, solutionDirectory, depth);
      resolvedFiles = result.Files;
      unresolvedTypeNames = result.UnresolvedTypeNames;
    }
  }

  var processedSources = new List<string>(resolvedFiles.Count);

  foreach (var filePath in resolvedFiles)
  {
    var sourceText = await fileSystem.File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
    var isSeed = seedPathSet.Contains(filePath);
    processedSources.Add(SourceProcessor.Process(sourceText, trim: trim && !isSeed));
  }

  var decompiledSources = new List<string>();

  if (includeCompiled && recursive && unresolvedTypeNames.Count > 0)
  {
    var solutionDirectory = SolutionFinder.FindSolutionDirectory(fileSystem, baseDirectory);

    if (solutionDirectory is not null)
    {
      var decompiled = AssemblyDecompiler.Decompile(unresolvedTypeNames, solutionDirectory);

      foreach (var source in decompiled)
      {
        decompiledSources.Add(SourceProcessor.Process(source));
      }
    }
  }

  if (output is null)
  {
    OutputFormatter.Write(processedSources, decompiledSources, Console.Out);
    WritePromptIfSet(promptValue, Console.Out);
  }
  else
  {
    await using var stream = output.Open(FileMode.Create, FileAccess.Write, FileShare.None);
    await using var writer = new StreamWriter(stream);
    OutputFormatter.Write(processedSources, decompiledSources, writer);
    WritePromptIfSet(promptValue, writer);
  }
});

return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);

static void WritePromptIfSet(string? promptValue, TextWriter writer)
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

