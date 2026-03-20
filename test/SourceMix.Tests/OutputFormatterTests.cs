namespace Hj.SourceMix.Tests;

public sealed class OutputFormatterTests
{
  [Fact]
  public void Write_SingleSource_WrapsInFencedCodeBlock()
  {
    using var writer = new StringWriter();

    OutputFormatter.Write(["public class Foo { }"], [], writer);
    var output = writer.ToString();

    Assert.Contains("```csharp", output);
    Assert.Contains("public class Foo { }", output);
    Assert.Contains("```", output);
  }

  [Fact]
  public void Write_MultipleSources_SeparatesWithBlankLine()
  {
    using var writer = new StringWriter();

    OutputFormatter.Write(["class A { }", "class B { }"], [], writer);
    var output = writer.ToString();

    Assert.Contains("class A", output);
    Assert.Contains("class B", output);

    var blockCount = output.Split("```csharp", StringSplitOptions.None).Length - 1;
    Assert.Equal(2, blockCount);
  }

  [Fact]
  public void Write_WithDecompiledSources_WritesHeading()
  {
    using var writer = new StringWriter();

    OutputFormatter.Write(["class A { }"], ["interface IFoo { }"], writer);
    var output = writer.ToString();

    Assert.Contains("## Decompiled Dependencies", output);
    Assert.Contains("interface IFoo", output);
  }

  [Fact]
  public void Write_EmptySourcesList_WritesNothing()
  {
    using var writer = new StringWriter();

    OutputFormatter.Write([], [], writer);
    var output = writer.ToString();

    Assert.Equal(string.Empty, output);
  }

  [Fact]
  public void Write_EmptyDecompiledList_NoHeading()
  {
    using var writer = new StringWriter();

    OutputFormatter.Write(["class A { }"], [], writer);
    var output = writer.ToString();

    Assert.DoesNotContain("## Decompiled Dependencies", output);
  }

  [Fact]
  public void Write_DecompiledWithNoSources_WritesHeadingOnly()
  {
    using var writer = new StringWriter();

    OutputFormatter.Write([], ["interface IFoo { }"], writer);
    var output = writer.ToString();

    Assert.Contains("## Decompiled Dependencies", output);
    Assert.Contains("interface IFoo", output);
    Assert.DoesNotContain("\n\n## Decompiled", output);
  }
}
