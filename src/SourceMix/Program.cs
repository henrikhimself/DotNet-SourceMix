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

var rootCommand = new RootCommand("Collects .NET C# source files into an LLM AI optimized Markdown file.")
{
  filesArgument,
  outputOption,
  recursiveOption,
  depthOption,
  includeCompiledOption,
};

rootCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
{
  var fileSystem = new FileSystem();
  var files = parseResult.GetValue(filesArgument) ?? [];
  var output = parseResult.GetValue(outputOption);
  var recursive = parseResult.GetValue(recursiveOption);
  var depth = parseResult.GetValue(depthOption);
  var includeCompiled = parseResult.GetValue(includeCompiledOption);

  var baseDirectory = Directory.GetCurrentDirectory();
  var resolvedFiles = GlobResolver.Resolve(fileSystem, files, baseDirectory);

  IReadOnlySet<string> unresolvedTypeNames = new HashSet<string>();

  if (recursive && resolvedFiles.Count > 0)
  {
    var solutionDirectory = SolutionFinder.FindSolutionDirectory(fileSystem, baseDirectory);

    if (solutionDirectory is not null)
    {
      var result = DependencyResolver.Resolve(fileSystem, resolvedFiles, solutionDirectory, depth);
      resolvedFiles = result.Files;
      unresolvedTypeNames = result.UnresolvedTypeNames;
    }
  }

  var processedSources = new List<string>(resolvedFiles.Count);

  foreach (var filePath in resolvedFiles)
  {
    var sourceText = await fileSystem.File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
    processedSources.Add(SourceProcessor.Process(sourceText));
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
  }
  else
  {
    await using var stream = output.Open(FileMode.Create, FileAccess.Write, FileShare.None);
    await using var writer = new StreamWriter(stream);
    OutputFormatter.Write(processedSources, decompiledSources, writer);
  }
});

return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);

