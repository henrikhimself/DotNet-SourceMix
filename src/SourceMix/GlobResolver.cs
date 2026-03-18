using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Hj.SourceMix;

internal static class GlobResolver
{
  private static readonly char[] GlobChars = ['*', '?', '{', '['];

  internal static IReadOnlyList<string> Resolve(IEnumerable<string> patterns, string baseDirectory)
  {
    var results = new LinkedList<string>();
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var pattern in patterns)
    {
      if (pattern.IndexOfAny(GlobChars) >= 0)
      {
        var matcher = new Matcher();
        matcher.AddInclude(pattern);

        var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(baseDirectory));
        var matchResult = matcher.Execute(directoryInfo);

        foreach (var match in matchResult.Files)
        {
          var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, match.Path));

          if (seen.Add(fullPath))
          {
            results.AddLast(fullPath);
          }
        }
      }
      else
      {
        var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, pattern));

        if (File.Exists(fullPath) && seen.Add(fullPath))
        {
          results.AddLast(fullPath);
        }
      }
    }

    return [.. results];
  }
}
