namespace Hj.SourceMix.Core;

public static class OutputFormatter
{
  public static void WriteSkills(IReadOnlyList<string> skillContents, TextWriter writer)
  {
    foreach (var content in skillContents)
    {
      writer.WriteLine(content);
      writer.WriteLine();
    }
  }

  public static void WriteCode(
    IReadOnlyList<string> processedSources,
    IReadOnlyList<string> decompiledSources,
    TextWriter writer)
  {
    if (processedSources.Count > 0)
    {
      writer.WriteLine("---");
      writer.WriteLine("# Source code");
      writer.WriteLine();
    }

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

  public static void WritePrompt(TextWriter writer, string promptText)
  {
    writer.WriteLine();
    writer.WriteLine("---");
    writer.WriteLine("# Instructions");
    writer.WriteLine();
    writer.WriteLine(promptText);
  }
}
