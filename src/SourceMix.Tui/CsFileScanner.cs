using System.IO.Abstractions;

namespace Hj.SourceMix.Tui;

internal sealed class CsFileScanner
{
  private readonly IFileSystem _fileSystem;
  private readonly string _solutionDirectory;
  private IReadOnlyList<CsFile>? _cache;

  internal CsFileScanner(IFileSystem fileSystem, string solutionDirectory)
  {
    _fileSystem = fileSystem;
    _solutionDirectory = solutionDirectory;
  }

  internal IReadOnlyList<CsFile> GetAllFiles()
  {
    if (_cache is not null)
    {
      return _cache;
    }

    var files = new List<CsFile>();

    foreach (var path in _fileSystem.Directory.EnumerateFiles(_solutionDirectory, "*.cs", SearchOption.AllDirectories))
    {
      var fullPath = _fileSystem.Path.GetFullPath(path);

      if (IsExcluded(fullPath))
      {
        continue;
      }

      var relativePath = _fileSystem.Path.GetRelativePath(_solutionDirectory, fullPath);
      files.Add(new CsFile(fullPath, relativePath));
    }

    files.Sort(static (a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.RelativePath, b.RelativePath));
    _cache = files;

    return _cache;
  }

  private static bool IsExcluded(string fullPath)
  {
    var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    foreach (var part in parts)
    {
      if (part.Equals("bin", StringComparison.OrdinalIgnoreCase)
        || part.Equals("obj", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }
}
