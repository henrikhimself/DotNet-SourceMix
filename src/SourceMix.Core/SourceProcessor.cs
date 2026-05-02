using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hj.SourceMix.Core;

public static class SourceProcessor
{
  public static string Process(string sourceText, bool trim = false)
  {
    var tree = CSharpSyntaxTree.ParseText(sourceText);
    var root = tree.GetRoot();

    var stripRewriter = new StripRewriter();
    var stripped = stripRewriter.Visit(root);

    if (trim)
    {
      var trimRewriter = new TrimRewriter();
      stripped = trimRewriter.Visit(stripped);
    }

    var result = stripped.ToFullString();

    return PostProcess(result);
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
