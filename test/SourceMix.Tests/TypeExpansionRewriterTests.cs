namespace Hj.SourceMix.Tests;

public sealed class TypeExpansionRewriterTests
{
  [Fact]
  public void ProcessBatch_ExpandsVarFromLiteral()
  {
    var result = Run(
      """
      namespace N;

      public static class C
      {
        public static void M()
        {
          var x = 1;
        }
      }
      """);

    Assert.Contains("int x = 1;", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_ExpandsVarFromGenericConstruction()
  {
    var result = Run(
      """
      using System.Collections.Generic;

      namespace N;

      public static class C
      {
        public static void M()
        {
          var list = new List<int>();
        }
      }
      """);

    Assert.Contains("List<int> list = new List<int>();", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_LeavesAnonymousTypesAsVar()
  {
    var result = Run(
      """
      namespace N;

      public static class C
      {
        public static void M()
        {
          var item = new { Id = 1 };
        }
      }
      """);

    Assert.Contains("var item = new { Id = 1 };", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_LeavesTupleDeconstructionAsVar()
  {
    var result = Run(
      """
      namespace N;

      public static class C
      {
        public static void M()
        {
          var (a, b) = (1, 2);
        }
      }
      """);

    Assert.Contains("var (a, b) = (1, 2);", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_ExpandsForeachVarElementType()
  {
    var result = Run(
      """
      using System.Collections.Generic;

      namespace N;

      public static class C
      {
        public static void M(List<int> values)
        {
          foreach (var value in values)
          {
          }
        }
      }
      """);

    Assert.Contains("foreach (int value in values) {", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_LeavesForeachDeconstructionUnchanged()
  {
    var result = Run(
      """
      namespace N;

      public static class C
      {
        public static void M((int, int)[] pairs)
        {
          foreach (var (left, right) in pairs)
          {
          }
        }
      }
      """);

    Assert.Contains("foreach (var (left, right) in pairs) {", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_ExpandsTargetTypedNewAssignment()
  {
    var result = Run(
      """
      namespace N;

      public sealed class Foo
      {
      }

      public static class C
      {
        public static void M()
        {
          Foo foo = new();
        }
      }
      """);

    Assert.Contains("Foo foo = new Foo();", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_ExpandsTargetTypedNewReturn()
  {
    var result = Run(
      """
      namespace N;

      public sealed class Foo
      {
      }

      public static class C
      {
        public static Foo Create() => new();
      }
      """);

    Assert.Contains("public static Foo Create() => new Foo();", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_ExpandsCrossFileTypes()
  {
    const string OtherFile = """
      namespace N;

      public sealed class MyType
      {
      }
      """;

    var result = Run(
      """
      namespace N;

      public static class C
      {
        public static void M()
        {
          var item = new MyType();
        }
      }
      """,
      ("MyType.cs", OtherFile));

    Assert.Contains("MyType item = new MyType();", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_LeavesUnresolvedTypesUnchanged()
  {
    var result = Run(
      """
      namespace N;

      public static class C
      {
        public static void M()
        {
          var value = Missing();
        }
      }
      """);

    Assert.Contains("var value = Missing();", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_ExpandsTupleAndNullableTypes()
  {
    var result = Run(
      """
      #nullable enable

      namespace N;

      public static class C
      {
        public static string? Maybe() => null;

        public static void M()
        {
          var tuple = (1, "x");
          var text = Maybe();
        }
      }
      """);

    Assert.Contains("(int, string) tuple = (1, \"x\");", result, StringComparison.Ordinal);
    Assert.Contains("string? text = Maybe();", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_ExpandsUsingVar()
  {
    var result = Run(
      """
      using System.IO;

      namespace N;

      public static class C
      {
        public static void M()
        {
          using var stream = new MemoryStream();
        }
      }
      """);

    Assert.Contains("using MemoryStream stream = new MemoryStream();", result, StringComparison.Ordinal);
  }

  [Fact]
  public void ProcessBatch_WhenExpandTypesIsFalse_LeavesVarAndNewUnchanged()
  {
    var result = Run(
      """
      namespace N;

      public sealed class Foo
      {
      }

      public static class C
      {
        public static void M()
        {
          var x = 1;
          Foo foo = new();
        }
      }
      """,
      expandTypes: false);

    Assert.Contains("var x = 1;", result, StringComparison.Ordinal);
    Assert.Contains("Foo foo = new();", result, StringComparison.Ordinal);
  }

  private static string Run(string source, params (string Path, string Text)[] additionalFiles)
    => Run(source, expandTypes: true, additionalFiles);

  private static string Run(string source, bool expandTypes, params (string Path, string Text)[] additionalFiles)
  {
    var files = new List<SourceFile>
    {
      new("Main.cs", source, true),
    };

    foreach (var (path, text) in additionalFiles)
    {
      files.Add(new SourceFile(path, text, false));
    }

    return SourceProcessor.ProcessBatch(files, expandTypes: expandTypes)[0];
  }
}
