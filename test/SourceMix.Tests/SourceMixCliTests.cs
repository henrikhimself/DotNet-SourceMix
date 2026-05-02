using System.IO.Abstractions.TestingHelpers;

namespace Hj.SourceMix.Tests;

public sealed class SourceMixCliTests
{
  [Fact]
  public async Task RunAsync_TrimWithoutRecursive_ReturnsErrorAsync()
  {
    var fileSystem = CreateFileSystem();
    using var errorWriter = new StringWriter();

    var exitCode = await SourceMixCli.RunAsync(
      new CliRequest(["Foo.cs"], null, false, int.MaxValue, false, true, null, []),
      fileSystem,
      "/workspace",
      TextWriter.Null,
      errorWriter);

    Assert.Equal(1, exitCode);
    Assert.Contains("--trim requires --recursive", errorWriter.ToString(), StringComparison.Ordinal);
  }

  [Fact]
  public async Task RunAsync_IncludeCompiledWithoutRecursive_ReturnsErrorAsync()
  {
    var fileSystem = CreateFileSystem();
    using var errorWriter = new StringWriter();

    var exitCode = await SourceMixCli.RunAsync(
      new CliRequest(["Foo.cs"], null, false, int.MaxValue, true, false, null, []),
      fileSystem,
      "/workspace",
      TextWriter.Null,
      errorWriter);

    Assert.Equal(1, exitCode);
    Assert.Contains("--include-compiled requires --recursive", errorWriter.ToString(), StringComparison.Ordinal);
  }

  [Fact]
  public async Task RunAsync_MissingOutputDirectory_ReturnsErrorAsync()
  {
    var fileSystem = CreateFileSystem();
    using var errorWriter = new StringWriter();

    var exitCode = await SourceMixCli.RunAsync(
      new CliRequest(["Foo.cs"], new FileInfo("/workspace/missing/context.md"), false, int.MaxValue, false, false, null, []),
      fileSystem,
      "/workspace",
      TextWriter.Null,
      errorWriter);

    Assert.Equal(1, exitCode);
    Assert.Contains("output directory does not exist", errorWriter.ToString(), StringComparison.Ordinal);
  }

  private static MockFileSystem CreateFileSystem()
  {
    return new MockFileSystem(
      new Dictionary<string, MockFileData>
      {
        ["/workspace/Foo.cs"] = new("public sealed class Foo { }"),
      },
      "/workspace");
  }
}
