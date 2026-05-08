using System.IO.Abstractions;

namespace Hj.SourceMix.Core;

/// <summary>
/// Centralised validation of run-time request rules. CLI and TUI consume this so that
/// error messages stay consistent across both surfaces.
/// </summary>
public static class RequestValidator
{
  public static IReadOnlyList<string> ValidateOptions(bool recursive, bool trim, bool includeCompiled)
  {
    var errors = new List<string>();

    if (trim && !recursive)
    {
      errors.Add("Error: --trim requires --recursive.");
    }

    if (includeCompiled && !recursive)
    {
      errors.Add("Error: --include-compiled requires --recursive.");
    }

    return errors;
  }

  public static bool TryValidateOutputDirectory(IFileSystem fileSystem, string? outputPath, out string? error)
  {
    error = null;

    if (string.IsNullOrEmpty(outputPath))
    {
      return true;
    }

    var directory = Path.GetDirectoryName(outputPath);

    if (string.IsNullOrEmpty(directory) || fileSystem.Directory.Exists(directory))
    {
      return true;
    }

    error = $"Error: output directory does not exist: {directory}";

    return false;
  }
}
