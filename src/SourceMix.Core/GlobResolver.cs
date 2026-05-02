using System.IO.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;
using GlobbingAbstractions = Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Hj.SourceMix.Core;

public static class GlobResolver
{
  private static readonly char[] _globChars = ['*', '?', '{', '['];

  public static IReadOnlyList<string> Resolve(IFileSystem fileSystem, IEnumerable<string> patterns, string baseDirectory)
  {
    var results = new LinkedList<string>();
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var pattern in patterns)
    {
      if (pattern.IndexOfAny(_globChars) >= 0)
      {
        var matcher = new Matcher();
        matcher.AddInclude(pattern);

        var directoryInfo = new AbstractionsDirectoryInfo(fileSystem.DirectoryInfo.New(baseDirectory));
        var matchResult = matcher.Execute(directoryInfo);

        foreach (var match in matchResult.Files)
        {
          var fullPath = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(baseDirectory, match.Path));

          if (seen.Add(fullPath))
          {
            results.AddLast(fullPath);
          }
        }
      }
      else
      {
        var fullPath = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(baseDirectory, pattern));

        if (fileSystem.File.Exists(fullPath) && seen.Add(fullPath))
        {
          results.AddLast(fullPath);
        }
      }
    }

    return [.. results];
  }

  private sealed class AbstractionsDirectoryInfo : GlobbingAbstractions.DirectoryInfoBase
  {
    private readonly IDirectoryInfo _info;

    internal AbstractionsDirectoryInfo(IDirectoryInfo info)
    {
      _info = info;
    }

    public override string Name => _info.Name;

    public override string FullName => _info.FullName;

    public override GlobbingAbstractions.DirectoryInfoBase? ParentDirectory =>
      _info.Parent is null ? null : new AbstractionsDirectoryInfo(_info.Parent);

    public override IEnumerable<GlobbingAbstractions.FileSystemInfoBase> EnumerateFileSystemInfos()
    {
      foreach (var dir in _info.EnumerateDirectories())
      {
        yield return new AbstractionsDirectoryInfo(dir);
      }

      foreach (var file in _info.EnumerateFiles())
      {
        yield return new AbstractionsFileInfo(file);
      }
    }

    public override GlobbingAbstractions.DirectoryInfoBase GetDirectory(string path)
    {
      var fs = _info.FileSystem;

      return new AbstractionsDirectoryInfo(fs.DirectoryInfo.New(fs.Path.Combine(FullName, path)));
    }

    public override GlobbingAbstractions.FileInfoBase GetFile(string path)
    {
      var fs = _info.FileSystem;

      return new AbstractionsFileInfo(fs.FileInfo.New(fs.Path.Combine(FullName, path)));
    }
  }

  private sealed class AbstractionsFileInfo : GlobbingAbstractions.FileInfoBase
  {
    private readonly IFileInfo _info;

    internal AbstractionsFileInfo(IFileInfo info)
    {
      _info = info;
    }

    public override string Name => _info.Name;

    public override string FullName => _info.FullName;

    public override GlobbingAbstractions.DirectoryInfoBase? ParentDirectory =>
      _info.Directory is null ? null : new AbstractionsDirectoryInfo(_info.Directory);
  }
}
