using System.IO.Abstractions.TestingHelpers;

namespace Hj.SourceMix.Tests;

public sealed class SolutionFinderTests
{
  [Fact]
  public void FindSolutionDirectory_SlnxInCurrentDir_ReturnsThatDirectory()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    fs.File.WriteAllText(fs.Path.Combine(root, "MySolution.slnx"), "<Solution />");

    var result = SolutionFinder.FindSolutionDirectory(fs, root);

    Assert.Equal(root, result);
  }

  [Fact]
  public void FindSolutionDirectory_SlnInParentDir_ReturnsParentDirectory()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    var subDir = fs.Path.Combine(root, "src", "MyProject");
    fs.Directory.CreateDirectory(subDir);
    fs.File.WriteAllText(fs.Path.Combine(root, "MySolution.sln"), string.Empty);

    var result = SolutionFinder.FindSolutionDirectory(fs, subDir);

    Assert.Equal(root, result);
  }

  [Fact]
  public void FindSolutionDirectory_NoSolutionFound_ReturnsNull()
  {
    var fs = new MockFileSystem();
    var isolated = fs.Path.Combine(fs.Path.GetTempPath(), "isolated");
    fs.Directory.CreateDirectory(isolated);

    var result = SolutionFinder.FindSolutionDirectory(fs, isolated);

    Assert.Null(result);
  }

  [Fact]
  public void FindSolutionDirectory_BothSlnAndSlnx_FindsEither()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    fs.File.WriteAllText(fs.Path.Combine(root, "MySolution.slnx"), "<Solution />");
    fs.File.WriteAllText(fs.Path.Combine(root, "MySolution.sln"), string.Empty);

    var result = SolutionFinder.FindSolutionDirectory(fs, root);

    Assert.Equal(root, result);
  }
}
