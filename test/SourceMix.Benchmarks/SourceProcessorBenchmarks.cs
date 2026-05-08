using System.Globalization;
using BenchmarkDotNet.Attributes;
using Hj.SourceMix.Core;

namespace Hj.SourceMix.Benchmarks;

[MemoryDiagnoser]
#pragma warning disable CA1515 // Consider making public types internal
public class SourceProcessorBenchmarks
#pragma warning restore CA1515 // Consider making public types internal
{
  private IReadOnlyList<SourceFile> _files = [];
  private string _source = string.Empty;

  [Params("small", "medium", "large")]
  public string SourceSize { get; set; } = "small";

  [Params(false, true)]
  public bool Trim { get; set; }

  [Params(false, true)]
  public bool ExpandTypes { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    _source = SourceSize switch
    {
      "small" => GenerateSmallSource(),
      "medium" => GenerateMediumSource(),
      _ => GenerateLargeSource(),
    };

    _files = [new SourceFile("Benchmark.cs", _source, true)];
  }

  [Benchmark]
  public object ProcessBatch() => SourceProcessor.ProcessBatch(_files, Trim, ExpandTypes);

  private static string GenerateSmallSource()
  {
    // ~80 lines: simple DTO/model — 5 properties, constructor, ToString
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("using System;");
    sb.AppendLine();
    sb.AppendLine("namespace Bench;");
    sb.AppendLine();
    sb.AppendLine("/// <summary>A simple data transfer object.</summary>");
    sb.AppendLine("public sealed class SmallDto");
    sb.AppendLine("{");

    for (var i = 0; i < 5; i++)
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"    /// <summary>Gets or sets the value of field {i}.</summary>");
      sb.AppendLine(CultureInfo.InvariantCulture, $"    public string Field{i} {{ get; set; }} = string.Empty;");
      sb.AppendLine();
    }

    sb.AppendLine("    /// <summary>Initializes a new instance of <see cref=\"SmallDto\"/>.</summary>");
    sb.AppendLine("    public SmallDto() { }");
    sb.AppendLine();
    sb.AppendLine("    /// <summary>Initializes a new instance with all fields.</summary>");
    sb.Append("    public SmallDto(");

    for (var i = 0; i < 5; i++)
    {
      if (i > 0)
      {
        sb.Append(", ");
      }

      sb.Append(CultureInfo.InvariantCulture, $"string field{i}");
    }

    sb.AppendLine(")");
    sb.AppendLine("    {");

    for (var i = 0; i < 5; i++)
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"        Field{i} = field{i};");
    }

    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    /// <inheritdoc/>");
    sb.AppendLine("    public override string ToString()");
    sb.AppendLine("    {");
    sb.Append("        return $\"SmallDto(");

    for (var i = 0; i < 5; i++)
    {
      if (i > 0)
      {
        sb.Append(", ");
      }

      sb.Append(CultureInfo.InvariantCulture, $"Field{i}={{Field{i}}}");
    }

    sb.AppendLine(")\";");
    sb.AppendLine("    }");
    sb.AppendLine("}");
    return sb.ToString();
  }

  private static string GenerateMediumSource()
  {
    // ~250 lines: typical service class — 10 properties, 10 methods with real bodies
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("using System;");
    sb.AppendLine("using System.Collections.Generic;");
    sb.AppendLine("using System.Linq;");
    sb.AppendLine();
    sb.AppendLine("namespace Bench;");
    sb.AppendLine();
    sb.AppendLine("/// <summary>A typical service class.</summary>");
    sb.AppendLine("public sealed class MediumService");
    sb.AppendLine("{");
    sb.AppendLine("    private readonly List<string> _items = [];");
    sb.AppendLine();

    for (var i = 0; i < 10; i++)
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"    /// <summary>Gets or sets configuration value {i}.</summary>");
      sb.AppendLine(CultureInfo.InvariantCulture, $"    public string Config{i} {{ get; set; }} = string.Empty;");
      sb.AppendLine();
    }

    sb.AppendLine("    /// <summary>Initializes a new instance.</summary>");
    sb.AppendLine("    public MediumService() { }");
    sb.AppendLine();

    for (var i = 0; i < 10; i++)
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"    /// <summary>Processes item at index {i}.</summary>");
      sb.AppendLine(CultureInfo.InvariantCulture, $"    public string Process{i}(string input)");
      sb.AppendLine("    {");
      sb.AppendLine(CultureInfo.InvariantCulture, $"        if (string.IsNullOrEmpty(input)) return Config{i};");
      sb.AppendLine(CultureInfo.InvariantCulture, $"        var result = input + Config{i};");
      sb.AppendLine("        _items.Add(result);");
      sb.AppendLine("        return result;");
      sb.AppendLine("    }");
      sb.AppendLine();
    }

    sb.AppendLine("    /// <summary>Returns all accumulated items.</summary>");
    sb.AppendLine("    public IReadOnlyList<string> GetAll()");
    sb.AppendLine("    {");
    sb.AppendLine("        return _items.Where(x => !string.IsNullOrEmpty(x)).ToList();");
    sb.AppendLine("    }");
    sb.AppendLine("}");
    return sb.ToString();
  }

  private static string GenerateLargeSource()
  {
    // ~500 lines: complex class — 20 properties, 20 methods with loops and branching
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("using System;");
    sb.AppendLine("using System.Collections.Generic;");
    sb.AppendLine("using System.Linq;");
    sb.AppendLine("using System.Text;");
    sb.AppendLine();
    sb.AppendLine("namespace Bench;");
    sb.AppendLine();
    sb.AppendLine("/// <summary>A complex class representative of a large real-world file.</summary>");
    sb.AppendLine("public sealed class LargeProcessor");
    sb.AppendLine("{");
    sb.AppendLine("    private readonly Dictionary<string, List<string>> _cache = new();");
    sb.AppendLine("    private int _operationCount;");
    sb.AppendLine();

    for (var i = 0; i < 20; i++)
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"    /// <summary>Gets or sets option {i}.</summary>");
      sb.AppendLine(CultureInfo.InvariantCulture, $"    public string Option{i} {{ get; set; }} = string.Empty;");
      sb.AppendLine();
    }

    sb.AppendLine("    /// <summary>Initializes a new instance.</summary>");
    sb.AppendLine("    public LargeProcessor() { }");
    sb.AppendLine();

    for (var i = 0; i < 20; i++)
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"    /// <summary>Executes operation {i} against the provided inputs.</summary>");
      sb.AppendLine(CultureInfo.InvariantCulture, $"    public string Execute{i}(IEnumerable<string> inputs, int maxItems)");
      sb.AppendLine("    {");
      sb.AppendLine(CultureInfo.InvariantCulture, $"        var key = nameof(Execute{i});");
      sb.AppendLine("        if (!_cache.TryGetValue(key, out var cached))");
      sb.AppendLine("        {");
      sb.AppendLine("            cached = [];");
      sb.AppendLine("            _cache[key] = cached;");
      sb.AppendLine("        }");
      sb.AppendLine();
      sb.AppendLine("        var sb = new StringBuilder();");
      sb.AppendLine("        var count = 0;");
      sb.AppendLine();
      sb.AppendLine("        foreach (var input in inputs)");
      sb.AppendLine("        {");
      sb.AppendLine("            if (count >= maxItems) break;");
      sb.AppendLine(CultureInfo.InvariantCulture, $"            var processed = input + Option{i};");
      sb.AppendLine("            if (!string.IsNullOrEmpty(processed))");
      sb.AppendLine("            {");
      sb.AppendLine("                sb.Append(processed);");
      sb.AppendLine("                cached.Add(processed);");
      sb.AppendLine("                count++;");
      sb.AppendLine("            }");
      sb.AppendLine("        }");
      sb.AppendLine();
      sb.AppendLine("        _operationCount++;");
      sb.AppendLine("        return sb.ToString();");
      sb.AppendLine("    }");
      sb.AppendLine();
    }

    sb.AppendLine("    /// <summary>Returns total operation count.</summary>");
    sb.AppendLine("    public int GetOperationCount() => _operationCount;");
    sb.AppendLine();
    sb.AppendLine("    /// <summary>Clears all cached results.</summary>");
    sb.AppendLine("    public void ClearCache() => _cache.Clear();");
    sb.AppendLine("}");
    return sb.ToString();
  }
}
