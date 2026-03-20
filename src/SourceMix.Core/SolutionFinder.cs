using System.IO.Abstractions;

namespace Hj.SourceMix.Core;

public static class SolutionFinder
{
  private static readonly string[] SolutionExtensions = [".slnx", ".sln"];

  public static string? FindSolutionDirectory(IFileSystem fileSystem, string startDirectory)
  {
    var directory = fileSystem.DirectoryInfo.New(startDirectory);

    while (directory is not null)
    {
      foreach (var extension in SolutionExtensions)
      {
        if (directory.GetFiles($"*{extension}").Length > 0)
        {
          return directory.FullName;
        }
      }

      directory = directory.Parent;
    }

    return null;
  }
}
