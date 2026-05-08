using System.IO.Abstractions;

namespace Hj.SourceMix.Core;

/// <summary>
/// Centralised filesystem-path normalization and comparison.
/// On Linux, paths are case-sensitive (ordinal). On Windows and macOS, paths are
/// compared case-insensitively. Use <see cref="Normalize(string)"/> before comparing
/// or storing path keys to ensure case, separator, and relative-segment differences are
/// resolved consistently.
/// </summary>
public static class PathNormalizer
{
  public static StringComparer Comparer { get; } = OperatingSystem.IsLinux()
    ? StringComparer.Ordinal
    : StringComparer.OrdinalIgnoreCase;

  public static StringComparison Comparison { get; } = OperatingSystem.IsLinux()
    ? StringComparison.Ordinal
    : StringComparison.OrdinalIgnoreCase;

  public static string Normalize(string path)
  {
    var full = Path.GetFullPath(path);
    return TrimTrailingSeparator(full);
  }

  public static string Normalize(IFileSystem fileSystem, string path)
  {
    var full = fileSystem.Path.GetFullPath(path);
    return TrimTrailingSeparator(full);
  }

  private static string TrimTrailingSeparator(string fullPath)
  {
    var root = Path.GetPathRoot(fullPath) ?? string.Empty;

    if (fullPath.Length <= root.Length)
    {
      return fullPath;
    }

    return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
  }
}
