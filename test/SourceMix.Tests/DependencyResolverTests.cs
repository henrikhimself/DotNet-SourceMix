using System.IO.Abstractions.TestingHelpers;

namespace Hj.SourceMix.Tests;

public sealed class DependencyResolverTests
{
  [Fact]
  public void Resolve_DirectDependency_IncludesDependentFile()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fileA = WriteFile(fs, root, "A.cs", "namespace T; public class A { B b; }");
    var fileB = WriteFile(fs, root, "B.cs", "namespace T; public class B { }");

    var result = DependencyResolver.Resolve(fs, [fileA], root, int.MaxValue);

    Assert.Contains(fileA, result.Files);
    Assert.Contains(fileB, result.Files);
  }

  [Fact]
  public void Resolve_TransitiveDependency_IncludesAllLevels()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fileA = WriteFile(fs, root, "A.cs", "namespace T; public class A { B b; }");
    var fileB = WriteFile(fs, root, "B.cs", "namespace T; public class B { C c; }");
    var fileC = WriteFile(fs, root, "C.cs", "namespace T; public class C { }");

    var result = DependencyResolver.Resolve(fs, [fileA], root, int.MaxValue);

    Assert.Contains(fileA, result.Files);
    Assert.Contains(fileB, result.Files);
    Assert.Contains(fileC, result.Files);
  }

  [Fact]
  public void Resolve_MaxDepthOne_DoesNotIncludeTransitiveDeps()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fileA = WriteFile(fs, root, "A.cs", "namespace T; public class A { B b; }");
    var fileB = WriteFile(fs, root, "B.cs", "namespace T; public class B { C c; }");
    var fileC = WriteFile(fs, root, "C.cs", "namespace T; public class C { }");

    var result = DependencyResolver.Resolve(fs, [fileA], root, maxDepth: 1);

    Assert.Contains(fileA, result.Files);
    Assert.Contains(fileB, result.Files);
    Assert.DoesNotContain(fileC, result.Files);
  }

  [Fact]
  public void Resolve_UnresolvableTypes_ReportsUnresolvedNames()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fileA = WriteFile(fs, root, "A.cs", "namespace T; public class A { IExternalService svc; }");

    var result = DependencyResolver.Resolve(fs, [fileA], root, int.MaxValue);

    Assert.Contains("IExternalService", result.UnresolvedTypeNames);
  }

  [Fact]
  public void Resolve_SeedFileAlwaysIncluded()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fileA = WriteFile(fs, root, "A.cs", "namespace T; public class A { }");

    var result = DependencyResolver.Resolve(fs, [fileA], root, int.MaxValue);

    Assert.Contains(fileA, result.Files);
  }

  [Fact]
  public void Resolve_StandaloneFile_ReturnsOnlySeedFile()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fileA = WriteFile(fs, root, "A.cs", "namespace T; public class A { }");
    WriteFile(fs, root, "B.cs", "namespace T; public class B { }");

    var result = DependencyResolver.Resolve(fs, [fileA], root, int.MaxValue);

    Assert.Single(result.Files);
    Assert.Equal(fileA, result.Files[0]);
  }

  private static string WriteFile(MockFileSystem fs, string directory, string name, string content)
  {
    var path = fs.Path.Combine(directory, name);
    fs.File.WriteAllText(path, content);

    return path;
  }
}
