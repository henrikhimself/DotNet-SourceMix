using System.CommandLine;
using Hj.SourceMix.Tui;

namespace Hj.SourceMix;

internal static class SourceMixCommandFactory
{
  public static RootCommand Create()
  {
    return Create(
      (request, cancellationToken) => SourceMixCli.RunAsync(request, cancellationToken: cancellationToken),
      SourceMixTuiApplication.RunAsync);
  }

  internal static RootCommand Create(
    Func<CliRequest, CancellationToken, Task<int>> cliRunner,
    Func<CancellationToken, Task<int>> tuiRunner)
  {
    ArgumentNullException.ThrowIfNull(cliRunner);
    ArgumentNullException.ThrowIfNull(tuiRunner);

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

    var expandTypesOption = new Option<bool>("--expand-types", "-e")
    {
      Description = "Replace 'var' with inferred types and expand target-typed new() to include the type. Anonymous and unresolved cases are left unchanged.",
    };

    var promptOption = new Option<string?>("--prompt", "-p")
    {
      Description = "Append a prompt personality to the output. Use a built-in key (nunit-test, xunit-test, code-review, tech-docs, explain, debug, refactor, architecture) or provide custom text.",
    };

    var skillsOption = new Option<string[]>("--skills", "-s")
    {
      Description = "One or more skill keys to prepend to the output. Each key is a subdirectory name in the skills config directory containing a SKILL.md file.",
      Arity = ArgumentArity.ZeroOrMore,
      AllowMultipleArgumentsPerToken = true,
    };

    var rootCommand = new RootCommand("Collects .NET C# source files into an LLM AI optimized Markdown file.")
    {
      filesArgument,
      outputOption,
      recursiveOption,
      depthOption,
      includeCompiledOption,
      trimOption,
      expandTypesOption,
      promptOption,
      skillsOption,
    };

    rootCommand.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
      cliRunner(
        new CliRequest(
          parseResult.GetValue(filesArgument) ?? [],
          parseResult.GetValue(outputOption),
          parseResult.GetValue(recursiveOption),
          parseResult.GetValue(depthOption),
          parseResult.GetValue(includeCompiledOption),
          parseResult.GetValue(trimOption),
          parseResult.GetValue(expandTypesOption),
          parseResult.GetValue(promptOption),
          parseResult.GetValue(skillsOption) ?? []),
        cancellationToken));

    var tuiCommand = new Command("tui", "Launch the interactive terminal wizard.");
    tuiCommand.SetAction((ParseResult _, CancellationToken cancellationToken) => tuiRunner(cancellationToken));
    rootCommand.Subcommands.Add(tuiCommand);

    return rootCommand;
  }
}
