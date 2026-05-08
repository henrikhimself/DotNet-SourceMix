using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hj.SourceMix.Core;

public static class SourceProcessor
{
  private static readonly Lazy<MetadataReference[]> _platformMetadataReferences = new(CreatePlatformMetadataReferences);

  public static string Process(string sourceText, bool trim = false)
  {
    var tree = CSharpSyntaxTree.ParseText(sourceText);

    return ProcessTree(tree, semanticModel: null, trim);
  }

  public static IReadOnlyList<string> ProcessBatch(
    IReadOnlyList<SourceFile> files,
    bool trim = false,
    bool expandTypes = false)
  {
    ArgumentNullException.ThrowIfNull(files);

    if (files.Count == 0)
    {
      return [];
    }

    var trees = new SyntaxTree[files.Count];

    for (var index = 0; index < files.Count; index++)
    {
      var file = files[index];
      ArgumentNullException.ThrowIfNull(file);

      trees[index] = CSharpSyntaxTree.ParseText(file.Text, path: file.Path);
    }

    CSharpCompilation? compilation = null;
    if (expandTypes)
    {
      compilation = CSharpCompilation.Create(
        "SourceMixBatch",
        trees,
        _platformMetadataReferences.Value,
        new CSharpCompilationOptions(
          OutputKind.DynamicallyLinkedLibrary,
          allowUnsafe: true));
    }

    var processed = new string[files.Count];

    for (var index = 0; index < files.Count; index++)
    {
      var semanticModel = compilation?.GetSemanticModel(trees[index], ignoreAccessibility: true);
      processed[index] = ProcessTree(trees[index], semanticModel, trim && !files[index].IsSeed);
    }

    return processed;
  }

  private static string PostProcess(string text)
  {
    var lines = text.Split('\n');
    var output = new List<string>(lines.Length);

    foreach (var rawLine in lines)
    {
      var line = rawLine.TrimEnd('\r').Replace("\t", string.Empty, StringComparison.Ordinal);

      if (string.IsNullOrWhiteSpace(line))
      {
        continue;
      }

      if (line.Trim() == "{" && output.Count > 0)
      {
        output[^1] += " {";
      }
      else
      {
        output.Add(line);
      }
    }

    return string.Join("\n", output);
  }

  private static string ProcessTree(SyntaxTree tree, SemanticModel? semanticModel, bool trim)
  {
    ArgumentNullException.ThrowIfNull(tree);

    var current = tree.GetRoot();

    if (semanticModel is not null)
    {
      current = new TypeExpansionRewriter(semanticModel).Visit(current) ?? current;
    }

    current = new StripRewriter().Visit(current) ?? current;

    if (trim)
    {
      current = new TrimRewriter().Visit(current) ?? current;
    }

    return PostProcess(current.ToFullString());
  }

  private static MetadataReference[] CreatePlatformMetadataReferences()
  {
    var references = new List<MetadataReference>();
    var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var trustedPlatformAssemblyPaths = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?
      .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
      ?? [];

    AddMetadataReferences(references, seenPaths, trustedPlatformAssemblyPaths);

    AddMetadataReferences(
      references,
      seenPaths,
      AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .Select(a => a.Location));

    return [.. references];
  }

  private static void AddMetadataReferences(
    List<MetadataReference> references,
    HashSet<string> seenPaths,
    IEnumerable<string> paths)
  {
    foreach (var path in paths)
    {
      if (!seenPaths.Add(path))
      {
        continue;
      }

      try
      {
        references.Add(MetadataReference.CreateFromFile(path));
      }
      catch (ArgumentException)
      {
      }
      catch (BadImageFormatException)
      {
      }
      catch (FileNotFoundException)
      {
      }
      catch (IOException)
      {
      }
      catch (UnauthorizedAccessException)
      {
      }
    }
  }

  private sealed class StripRewriter : CSharpSyntaxRewriter
  {
    public StripRewriter()
      : base(visitIntoStructuredTrivia: true)
    {
    }

    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node) => null;

    public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
    {
      return trivia.Kind() switch
      {
        SyntaxKind.SingleLineCommentTrivia => SyntaxFactory.ElasticMarker,
        SyntaxKind.MultiLineCommentTrivia => SyntaxFactory.ElasticMarker,
        SyntaxKind.SingleLineDocumentationCommentTrivia => SyntaxFactory.ElasticMarker,
        SyntaxKind.MultiLineDocumentationCommentTrivia => SyntaxFactory.ElasticMarker,
        SyntaxKind.DocumentationCommentExteriorTrivia => SyntaxFactory.ElasticMarker,
        _ => base.VisitTrivia(trivia),
      };
    }
  }

  private sealed class TrimRewriter : CSharpSyntaxRewriter
  {
    private static BlockSyntax EmptyBlock => SyntaxFactory.Block();

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
      if (node.Body is not null)
      {
        node = node.WithBody(EmptyBlock);
      }
      else if (node.ExpressionBody is not null)
      {
        node = node.WithExpressionBody(null).WithSemicolonToken(default).WithBody(EmptyBlock);
      }

      return base.VisitMethodDeclaration(node);
    }

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
      if (node.Body is not null)
      {
        node = node.WithBody(EmptyBlock);
      }
      else if (node.ExpressionBody is not null)
      {
        node = node.WithExpressionBody(null).WithSemicolonToken(default).WithBody(EmptyBlock);
      }

      return base.VisitConstructorDeclaration(node);
    }

    public override SyntaxNode? VisitDestructorDeclaration(DestructorDeclarationSyntax node)
    {
      if (node.Body is not null)
      {
        node = node.WithBody(EmptyBlock);
      }
      else if (node.ExpressionBody is not null)
      {
        node = node.WithExpressionBody(null).WithSemicolonToken(default).WithBody(EmptyBlock);
      }

      return base.VisitDestructorDeclaration(node);
    }

    public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node)
    {
      if (node.Body is not null)
      {
        node = node.WithBody(EmptyBlock);
      }
      else if (node.ExpressionBody is not null)
      {
        node = node.WithExpressionBody(null).WithSemicolonToken(default).WithBody(EmptyBlock);
      }

      return base.VisitOperatorDeclaration(node);
    }

    public override SyntaxNode? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
    {
      if (node.Body is not null)
      {
        node = node.WithBody(EmptyBlock);
      }
      else if (node.ExpressionBody is not null)
      {
        node = node.WithExpressionBody(null).WithSemicolonToken(default).WithBody(EmptyBlock);
      }

      return base.VisitConversionOperatorDeclaration(node);
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
      if (node.ExpressionBody is not null)
      {
        var getAccessor = SyntaxFactory
          .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
          .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        node = node
          .WithExpressionBody(null)
          .WithSemicolonToken(default)
          .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getAccessor)));
      }

      return base.VisitPropertyDeclaration(node);
    }

    public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
      if (node.ExpressionBody is not null)
      {
        var getAccessor = SyntaxFactory
          .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
          .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        node = node
          .WithExpressionBody(null)
          .WithSemicolonToken(default)
          .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getAccessor)));
      }

      return base.VisitIndexerDeclaration(node);
    }

    public override SyntaxNode? VisitAccessorDeclaration(AccessorDeclarationSyntax node)
    {
      if (node.Body is not null || node.ExpressionBody is not null)
      {
        return node
          .WithBody(null)
          .WithExpressionBody(null)
          .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
      }

      return base.VisitAccessorDeclaration(node);
    }
  }
}
