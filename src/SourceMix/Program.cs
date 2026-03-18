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

var rootCommand = new RootCommand("Collects .NET C# source files into an LLM AI optimized Markdown file.")
{
  filesArgument,
  outputOption,
};

rootCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
{
  var files = parseResult.GetValue(filesArgument) ?? [];
  var output = parseResult.GetValue(outputOption);

  var baseDirectory = Directory.GetCurrentDirectory();
  var resolvedFiles = GlobResolver.Resolve(files, baseDirectory);

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

