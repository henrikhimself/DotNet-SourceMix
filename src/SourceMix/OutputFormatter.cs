namespace Hj.SourceMix;

internal static class OutputFormatter
{
  internal static void Write(IReadOnlyList<string> processedSources, TextWriter writer)
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
  }
}
