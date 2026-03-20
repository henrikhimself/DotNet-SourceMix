using System.IO.Abstractions.TestingHelpers;

namespace Hj.SourceMix.Tests;

public sealed class GlobResolverTests
{
  [Fact]
  public void Resolve_ExactFilePath_ReturnsThatFile()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fooPath = fs.Path.Combine(root, "Foo.cs");
    fs.File.WriteAllText(fooPath, "// Foo");

    var result = GlobResolver.Resolve(fs, [fooPath], root);

    Assert.Single(result);
    Assert.Equal(fooPath, result[0]);
  }

  [Fact]
  public void Resolve_GlobPattern_ReturnsAllMatchingFiles()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    var subDir = fs.Path.Combine(root, "Sub");
    fs.Directory.CreateDirectory(subDir);
    fs.File.WriteAllText(fs.Path.Combine(root, "Foo.cs"), "// Foo");
    fs.File.WriteAllText(fs.Path.Combine(root, "Bar.cs"), "// Bar");
    fs.File.WriteAllText(fs.Path.Combine(subDir, "Baz.cs"), "// Baz");

    var result = GlobResolver.Resolve(fs, ["**/*.cs"], root);

    Assert.Equal(3, result.Count);
    Assert.Contains(result, f => f.EndsWith("Foo.cs", StringComparison.OrdinalIgnoreCase));
    Assert.Contains(result, f => f.EndsWith("Bar.cs", StringComparison.OrdinalIgnoreCase));
    Assert.Contains(result, f => f.EndsWith("Baz.cs", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void Resolve_DuplicatePatterns_DeduplicatesResults()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fooPath = fs.Path.Combine(root, "Foo.cs");
    fs.File.WriteAllText(fooPath, "// Foo");

    var result = GlobResolver.Resolve(fs, [fooPath, fooPath], root);

    Assert.Single(result);
  }

  [Fact]
  public void Resolve_NoMatchingPattern_ReturnsEmpty()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    fs.File.WriteAllText(fs.Path.Combine(root, "Foo.cs"), "// Foo");

    var result = GlobResolver.Resolve(fs, ["**/*.vb"], root);

    Assert.Empty(result);
  }

  [Fact]
  public void Resolve_NonExistentLiteralPath_IsIgnored()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var missingPath = fs.Path.Combine(root, "Missing.cs");

    var result = GlobResolver.Resolve(fs, [missingPath], root);

    Assert.Empty(result);
  }
}
