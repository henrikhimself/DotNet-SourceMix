namespace Hj.SourceMix;

internal static class SolutionFinder
{
  private static readonly string[] SolutionExtensions = [".slnx", ".sln"];

  internal static string? FindSolutionDirectory(string startDirectory)
  {
    var directory = new DirectoryInfo(startDirectory);

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
