using System.CommandLine;
using Hj.SourceMix;

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

var rootCommand = new RootCommand("Collects .NET C# source files into an LLM AI optimized Markdown file.")
{
  filesArgument,
  outputOption,
  recursiveOption,
  depthOption,
};

rootCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
{
  var files = parseResult.GetValue(filesArgument) ?? [];
  var output = parseResult.GetValue(outputOption);
  var recursive = parseResult.GetValue(recursiveOption);
  var depth = parseResult.GetValue(depthOption);

  var baseDirectory = Directory.GetCurrentDirectory();
  var resolvedFiles = GlobResolver.Resolve(files, baseDirectory);

  if (recursive && resolvedFiles.Count > 0)
  {
    var solutionDirectory = SolutionFinder.FindSolutionDirectory(baseDirectory);

    if (solutionDirectory is not null)
    {
      resolvedFiles = DependencyResolver.Resolve(resolvedFiles, solutionDirectory, depth);
    }
  }

  var processedSources = new List<string>(resolvedFiles.Count);

  foreach (var filePath in resolvedFiles)
  {
    var sourceText = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
    processedSources.Add(SourceProcessor.Process(sourceText));
  }

  if (output is null)
  {
    OutputFormatter.Write(processedSources, Console.Out);
  }
  else
  {
    await using var stream = output.Open(FileMode.Create, FileAccess.Write, FileShare.None);
    await using var writer = new StreamWriter(stream);
    OutputFormatter.Write(processedSources, writer);
  }
});

return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);

