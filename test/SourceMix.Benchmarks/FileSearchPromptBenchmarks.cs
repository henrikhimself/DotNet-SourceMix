using BenchmarkDotNet.Attributes;
using Hj.SourceMix.Tui;

namespace Hj.SourceMix.Benchmarks;

[MemoryDiagnoser]
#pragma warning disable CA1515 // Consider making public types internal
public class FileSearchPromptBenchmarks
#pragma warning restore CA1515 // Consider making public types internal
{
  private const string Query = "Service";
  private const int NavigationKeypresses = 30;

  private IReadOnlyList<CsFile> _files = [];

  [Params(100, 1000, 5000)]
  public int Files { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    var files = new List<CsFile>(Files);

    for (var i = 0; i < Files; i++)
    {
      var name = i % 5 == 0
        ? $"src/Feature{i}/ServiceHandler{i}.cs"
        : $"src/Feature{i}/Model{i}.cs";
      files.Add(new CsFile($"/repo/{name}", name));
    }

    _files = files;
  }

  // Simulates typing Query one character at a time.
  // Old behavior: Filter(allFiles) called once per character regardless.
  [Benchmark(Baseline = true)]
  public object TypeQuerySequential()
  {
    object result = _files;

    for (var i = 1; i <= Query.Length; i++)
    {
      result = ApplyFilter(_files, Query[..i]);
    }

    return result;
  }

  // Simulates typing Query one character at a time.
  // New behavior: each character filters the previous result (incremental).
  [Benchmark]
  public object TypeQueryIncremental()
  {
    var current = _files;
    var lastSearch = string.Empty;

    for (var i = 1; i <= Query.Length; i++)
    {
      var partial = Query[..i];
      var source = partial.StartsWith(lastSearch, StringComparison.OrdinalIgnoreCase)
        ? current
        : (IReadOnlyList<CsFile>)_files;
      current = ApplyFilter(source, partial);
      lastSearch = partial;
    }

    return current;
  }

  // Simulates NavigationKeypresses arrow-key presses with a fixed query active.
  // Old behavior: Filter(allFiles) called on every keypress.
  [Benchmark]
  public object NavigateUncached()
  {
    object result = _files;

    for (var i = 0; i < NavigationKeypresses; i++)
    {
      result = ApplyFilter(_files, Query);
    }

    return result;
  }

  // Simulates NavigationKeypresses arrow-key presses with a fixed query active.
  // New behavior: Filter called once; cached result reused for subsequent keypresses.
  [Benchmark]
  public object NavigateCached()
  {
    IReadOnlyList<CsFile>? cached = null;
    var lastSearch = (string?)null;

    for (var i = 0; i < NavigationKeypresses; i++)
    {
      if (lastSearch != Query)
      {
        cached = ApplyFilter(_files, Query);
        lastSearch = Query;
      }
    }

    return cached ?? _files;
  }

  private static IReadOnlyList<CsFile> ApplyFilter(IReadOnlyList<CsFile> files, string search)
  {
    if (string.IsNullOrEmpty(search))
    {
      return files;
    }

    var result = new List<CsFile>();

    foreach (var file in files)
    {
      if (file.RelativePath.Contains(search, StringComparison.OrdinalIgnoreCase))
      {
        result.Add(file);
      }
    }

    return result;
  }
}
