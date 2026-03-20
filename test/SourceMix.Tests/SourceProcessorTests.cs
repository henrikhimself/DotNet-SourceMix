namespace Hj.SourceMix.Tests;

public sealed class SourceProcessorTests
{
  [Fact]
  public void Process_RemovesUsingDirectives()
  {
    var source = """
      using System;
      using System.Collections.Generic;

      namespace MyApp;

      public class Foo { }
      """;

    var result = SourceProcessor.Process(source);

    Assert.DoesNotContain("using", result, StringComparison.Ordinal);
  }

  [Fact]
  public void Process_RemovesSingleLineComments()
  {
    var source = """
      namespace MyApp;

      // This is a comment
      public class Foo
      {
        // Another comment
        public int Value { get; set; }
      }
      """;

    var result = SourceProcessor.Process(source);

    Assert.DoesNotContain("//", result, StringComparison.Ordinal);
    Assert.Contains("public class Foo", result, StringComparison.Ordinal);
    Assert.Contains("public int Value", result, StringComparison.Ordinal);
  }

  [Fact]
  public void Process_RemovesMultiLineComments()
  {
    var source = """
      namespace MyApp;

      /* This is a
         multi-line comment */
      public class Foo { }
      """;

    var result = SourceProcessor.Process(source);

    Assert.DoesNotContain("/*", result, StringComparison.Ordinal);
    Assert.DoesNotContain("*/", result, StringComparison.Ordinal);
    Assert.Contains("public class Foo", result, StringComparison.Ordinal);
  }

  [Fact]
  public void Process_RemovesXmlDocComments()
  {
    var source = """
      namespace MyApp;

      /// <summary>
      /// A class.
      /// </summary>
      public class Foo
      {
        /// <param name="x">A value.</param>
        public void Bar(int x) { }
      }
      """;

    var result = SourceProcessor.Process(source);

    Assert.DoesNotContain("///", result, StringComparison.Ordinal);
    Assert.DoesNotContain("<summary>", result, StringComparison.Ordinal);
    Assert.Contains("public class Foo", result, StringComparison.Ordinal);
    Assert.Contains("public void Bar", result, StringComparison.Ordinal);
  }

  [Fact]
  public void Process_CollapsesOpeningBraceOntoSameLine()
  {
    var source = """
      namespace MyApp;

      public class Foo
      {
        public void Bar()
        {
        }
      }
      """;

    var result = SourceProcessor.Process(source);

    Assert.Contains("public class Foo {", result, StringComparison.Ordinal);
    Assert.Contains("public void Bar() {", result, StringComparison.Ordinal);
  }

  [Fact]
  public void Process_RemovesBlankLines()
  {
    var source = """
      namespace MyApp;


      public class Foo
      {

        public int X { get; set; }

      }
      """;

    var result = SourceProcessor.Process(source);
    var lines = result.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

    Assert.Equal(lines.Count, result.Split('\n').Count(l => !string.IsNullOrWhiteSpace(l)));
  }

  [Fact]
  public void Process_PreservesNamespaceAndTypeDeclarations()
  {
    var source = """
      namespace MyApp.Services;

      public interface IOrderService
      {
        void PlaceOrder(int orderId);
      }
      """;

    var result = SourceProcessor.Process(source);

    Assert.Contains("namespace MyApp.Services", result, StringComparison.Ordinal);
    Assert.Contains("public interface IOrderService", result, StringComparison.Ordinal);
    Assert.Contains("void PlaceOrder(int orderId)", result, StringComparison.Ordinal);
  }

  [Fact]
  public void Process_HandlesEmptyInput()
  {
    var result = SourceProcessor.Process(string.Empty);

    Assert.Equal(string.Empty, result);
  }
}
