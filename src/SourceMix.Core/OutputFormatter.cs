namespace Hj.SourceMix.Core;

public static class OutputFormatter
{
  public static void Write(
    IReadOnlyList<string> processedSources,
    IReadOnlyList<string> decompiledSources,
    TextWriter writer)
  {
    for (var i = 0; i < processedSources.Count; i++)
    {
      if (i > 0)
      {
        writer.WriteLine();
      }

      writer.WriteLine("```csharp");
      writer.WriteLine(processedSources[i]);
      writer.WriteLine("```");
    }

    if (decompiledSources.Count > 0)
    {
      if (processedSources.Count > 0)
      {
        writer.WriteLine();
      }

      writer.WriteLine("## Decompiled Dependencies");

      foreach (var source in decompiledSources)
      {
        writer.WriteLine();
        writer.WriteLine("```csharp");
        writer.WriteLine(source);
        writer.WriteLine("```");
      }
    }
  }
}
