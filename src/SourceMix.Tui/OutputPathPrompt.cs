using System.IO.Abstractions;

namespace Hj.SourceMix.Tui;

internal static class OutputPathPrompt
{
  internal static OutputPathPromptResult Show(
    string initialPath,
    ITuiConsole? tuiConsole = null,
    IKeyReader? keys = null,
    IFileSystem? fileSystem = null)
  {
    ArgumentNullException.ThrowIfNull(initialPath);

    tuiConsole ??= new SystemTuiConsole();
    keys ??= new ConsoleKeyReader();
    var fs = fileSystem ?? new FileSystem();

    var (path, ok, quit) = TextInputPrompt.Read(
      "Output file:",
      initialPath,
      tuiConsole,
      keys,
      p =>
      {
        if (string.IsNullOrWhiteSpace(p))
        {
          return "Output path cannot be empty.";
        }

        var dir = Path.GetDirectoryName(p);

        return string.IsNullOrEmpty(dir) || fs.Directory.Exists(dir)
          ? null
          : $"Directory does not exist: {dir}";
      });

    if (quit)
    {
      return new OutputPathPromptResult(null, StepResult.Quit);
    }

    if (!ok)
    {
      return new OutputPathPromptResult(null, StepResult.Back);
    }

    return new OutputPathPromptResult(path, StepResult.Confirm);
  }
}
