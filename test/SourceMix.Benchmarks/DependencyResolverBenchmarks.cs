using System.IO.Abstractions;
using BenchmarkDotNet.Attributes;
using Hj.SourceMix.Core;

namespace Hj.SourceMix.Benchmarks;

[MemoryDiagnoser]
#pragma warning disable CA1515 // Consider making public types internal
public class DependencyResolverBenchmarks
#pragma warning restore CA1515 // Consider making public types internal
{
  private string _tempDir = string.Empty;
  private string _seedFile = string.Empty;

  [Params("chain", "island")]
  public string Topology { get; set; } = "chain";

  [Params(100, 500, 1000)]
  public int Files { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"sourcemix-bench-{Guid.NewGuid()}");
    Directory.CreateDirectory(_tempDir);

    for (var i = 0; i < Files; i++)
    {
      string content;
      var isIslandFile = Topology == "island" && i >= 50;
      if (!isIslandFile && i < Files - 1 && !(Topology == "island" && i == 49))
      {
        content = $"namespace Bench; public class Class{i} {{ Class{i + 1} _next; }}";
      }
      else
      {
        content = $"namespace Bench; public class Class{i} {{ }}";
      }

      File.WriteAllText(Path.Combine(_tempDir, $"Class{i}.cs"), content);
    }

    _seedFile = Path.Combine(_tempDir, "Class0.cs");
  }

  [GlobalCleanup]
  public void Cleanup()
  {
    if (Directory.Exists(_tempDir))
    {
      Directory.Delete(_tempDir, recursive: true);
    }
  }

  [Benchmark]
  public object Resolve()
  {
    var fs = new FileSystem();
    return DependencyResolver.Resolve(fs, [_seedFile], _tempDir, int.MaxValue);
  }
}
