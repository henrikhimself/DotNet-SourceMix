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

  [Fact]
  public void Resolve_FileUnderBinDirectory_IsNotIndexed()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fileA = WriteFile(fs, root, "A.cs", "namespace T; public class A { ExternalType field; }");
    var binDir = fs.Path.Combine(root, "bin", "Debug");
    fs.Directory.CreateDirectory(binDir);
    WriteFile(fs, binDir, "ExternalType.cs", "namespace T; public class ExternalType { }");

    var result = DependencyResolver.Resolve(fs, [fileA], root, int.MaxValue);

    Assert.DoesNotContain(result.Files, f => f.Contains(fs.Path.Combine("bin", "Debug"), StringComparison.OrdinalIgnoreCase));
    Assert.Contains("ExternalType", result.UnresolvedTypeNames);
  }

  [Fact]
  public void Resolve_FileUnderObjDirectory_IsNotIndexed()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fileA = WriteFile(fs, root, "A.cs", "namespace T; public class A { ExternalType field; }");
    var objDir = fs.Path.Combine(root, "obj", "Release");
    fs.Directory.CreateDirectory(objDir);
    WriteFile(fs, objDir, "ExternalType.cs", "namespace T; public class ExternalType { }");

    var result = DependencyResolver.Resolve(fs, [fileA], root, int.MaxValue);

    Assert.DoesNotContain(result.Files, f => f.Contains(fs.Path.Combine("obj", "Release"), StringComparison.OrdinalIgnoreCase));
    Assert.Contains("ExternalType", result.UnresolvedTypeNames);
  }

  [Fact]
  public void Resolve_MultipleSeedFiles_SeedOrderingPreserved()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fileA = WriteFile(fs, root, "A.cs", "namespace T; public class A { }");
    var fileB = WriteFile(fs, root, "B.cs", "namespace T; public class B { }");
    var fileC = WriteFile(fs, root, "C.cs", "namespace T; public class C { }");

    var result = DependencyResolver.Resolve(fs, [fileA, fileB, fileC], root, int.MaxValue);

    Assert.Equal(fileA, result.Files[0]);
    Assert.Equal(fileB, result.Files[1]);
    Assert.Equal(fileC, result.Files[2]);
  }

  [Fact]
  public void Resolve_DeepChain_AllFilesResolved()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fileA = WriteFile(fs, root, "A.cs", "namespace T; public class A { B b; }");
    var fileB = WriteFile(fs, root, "B.cs", "namespace T; public class B { C c; }");
    var fileC = WriteFile(fs, root, "C.cs", "namespace T; public class C { D d; }");
    var fileD = WriteFile(fs, root, "D.cs", "namespace T; public class D { E e; }");
    var fileE = WriteFile(fs, root, "E.cs", "namespace T; public class E { }");

    var result = DependencyResolver.Resolve(fs, [fileA], root, int.MaxValue);

    Assert.Contains(fileA, result.Files);
    Assert.Contains(fileB, result.Files);
    Assert.Contains(fileC, result.Files);
    Assert.Contains(fileD, result.Files);
    Assert.Contains(fileE, result.Files);
  }

  [Fact]
  public void Resolve_UnresolvableTypes_AllReported()
  {
    var fs = new MockFileSystem();
    var root = fs.Path.Combine(fs.Path.GetTempPath(), "test");
    fs.Directory.CreateDirectory(root);
    var fileA = WriteFile(fs, root, "A.cs", "namespace T; public class A { IFoo foo; IBar bar; }");

    var result = DependencyResolver.Resolve(fs, [fileA], root, int.MaxValue);

    Assert.Contains("IFoo", result.UnresolvedTypeNames);
    Assert.Contains("IBar", result.UnresolvedTypeNames);
  }

  private static string WriteFile(MockFileSystem fs, string directory, string name, string content)
  {
    var path = fs.Path.Combine(directory, name);
    fs.File.WriteAllText(path, content);

    return path;
  }
}
