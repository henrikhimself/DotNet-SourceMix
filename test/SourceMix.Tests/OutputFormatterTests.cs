namespace Hj.SourceMix.Tests;

public sealed class OutputFormatterTests
{
  [Fact]
  public void Write_SingleSource_WrapsInFencedCodeBlock()
  {
    using var writer = new StringWriter();

    OutputFormatter.WriteCode(["public class Foo { }"], [], writer);
    var output = writer.ToString();

    Assert.Contains("```csharp", output, StringComparison.Ordinal);
    Assert.Contains("public class Foo { }", output, StringComparison.Ordinal);
    Assert.Contains("```", output, StringComparison.Ordinal);
  }

  [Fact]
  public void Write_MultipleSources_SeparatesWithBlankLine()
  {
    using var writer = new StringWriter();

    OutputFormatter.WriteCode(["class A { }", "class B { }"], [], writer);
    var output = writer.ToString();

    Assert.Contains("class A", output, StringComparison.Ordinal);
    Assert.Contains("class B", output, StringComparison.Ordinal);

    var blockCount = output.Split("```csharp", StringSplitOptions.None).Length - 1;
    Assert.Equal(2, blockCount);
  }

  [Fact]
  public void Write_WithDecompiledSources_WritesHeading()
  {
    using var writer = new StringWriter();

    OutputFormatter.WriteCode(["class A { }"], ["interface IFoo { }"], writer);
    var output = writer.ToString();

    Assert.Contains("## Decompiled Dependencies", output, StringComparison.Ordinal);
    Assert.Contains("interface IFoo", output, StringComparison.Ordinal);
  }

  [Fact]
  public void Write_EmptySourcesList_WritesNothing()
  {
    using var writer = new StringWriter();

    OutputFormatter.WriteCode([], [], writer);
    var output = writer.ToString();

    Assert.Equal(string.Empty, output);
  }

  [Fact]
  public void Write_EmptyDecompiledList_NoHeading()
  {
    using var writer = new StringWriter();

    OutputFormatter.WriteCode(["class A { }"], [], writer);
    var output = writer.ToString();

    Assert.DoesNotContain("## Decompiled Dependencies", output, StringComparison.Ordinal);
  }

  [Fact]
  public void Write_DecompiledWithNoSources_WritesHeadingOnly()
  {
    using var writer = new StringWriter();

    OutputFormatter.WriteCode([], ["interface IFoo { }"], writer);
    var output = writer.ToString();

    Assert.Contains("## Decompiled Dependencies", output, StringComparison.Ordinal);
    Assert.Contains("interface IFoo", output, StringComparison.Ordinal);
    Assert.DoesNotContain("\n\n## Decompiled", output, StringComparison.Ordinal);
  }
}
