using System.IO.Abstractions.TestingHelpers;

namespace Hj.SourceMix.Tui.Tests;

public sealed class CsFileScannerTests
{
  [Fact]
  public void GetAllFiles_FindsCsFilesRecursively()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    var subDir = fs.Path.Combine(root, "Sub");
    fs.Directory.CreateDirectory(subDir);
    fs.File.WriteAllText(fs.Path.Combine(root, "Foo.cs"), string.Empty);
    fs.File.WriteAllText(fs.Path.Combine(subDir, "Bar.cs"), string.Empty);

    var scanner = new CsFileScanner(fs, root);
    var files = scanner.GetAllFiles();

    Assert.Equal(2, files.Count);
  }

  [Fact]
  public void GetAllFiles_ExcludesBinDirectory()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    var binDir = fs.Path.Combine(root, "bin");
    fs.Directory.CreateDirectory(binDir);
    fs.File.WriteAllText(fs.Path.Combine(root, "Foo.cs"), string.Empty);
    fs.File.WriteAllText(fs.Path.Combine(binDir, "Bar.cs"), string.Empty);

    var scanner = new CsFileScanner(fs, root);
    var files = scanner.GetAllFiles();

    Assert.Single(files);
    Assert.DoesNotContain(files, f => f.FullPath.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void GetAllFiles_ExcludesObjDirectory()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    var objDir = fs.Path.Combine(root, "obj");
    fs.Directory.CreateDirectory(objDir);
    fs.File.WriteAllText(fs.Path.Combine(root, "Foo.cs"), string.Empty);
    fs.File.WriteAllText(fs.Path.Combine(objDir, "GeneratedFile.cs"), string.Empty);

    var scanner = new CsFileScanner(fs, root);
    var files = scanner.GetAllFiles();

    Assert.Single(files);
  }

  [Fact]
  public void GetAllFiles_ReturnsRelativePaths()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    fs.File.WriteAllText(fs.Path.Combine(root, "Foo.cs"), string.Empty);

    var scanner = new CsFileScanner(fs, root);
    var files = scanner.GetAllFiles();

    Assert.Single(files);
    Assert.Equal("Foo.cs", files[0].RelativePath);
  }

  [Fact]
  public void GetAllFiles_ReturnsSortedResults()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    fs.File.WriteAllText(fs.Path.Combine(root, "Zebra.cs"), string.Empty);
    fs.File.WriteAllText(fs.Path.Combine(root, "Alpha.cs"), string.Empty);
    fs.File.WriteAllText(fs.Path.Combine(root, "Monkey.cs"), string.Empty);

    var scanner = new CsFileScanner(fs, root);
    var files = scanner.GetAllFiles();

    var names = files.Select(f => f.RelativePath).ToList();
    var sorted = names.Order(StringComparer.OrdinalIgnoreCase).ToList();

    Assert.Equal(sorted, names);
  }

  [Fact]
  public void GetAllFiles_CachesResultsOnSecondCall()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    fs.File.WriteAllText(fs.Path.Combine(root, "Foo.cs"), string.Empty);

    var scanner = new CsFileScanner(fs, root);
    var firstResult = scanner.GetAllFiles();

    fs.File.WriteAllText(fs.Path.Combine(root, "Bar.cs"), string.Empty);
    var secondResult = scanner.GetAllFiles();

    Assert.Same(firstResult, secondResult);
    Assert.Single(secondResult);
  }

  [Fact]
  public void GetAllFiles_EmptyDirectory_ReturnsEmpty()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);

    var scanner = new CsFileScanner(fs, root);
    var files = scanner.GetAllFiles();

    Assert.Empty(files);
  }
}
