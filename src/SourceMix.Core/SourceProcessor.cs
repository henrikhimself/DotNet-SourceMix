using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hj.SourceMix.Core;

public static class SourceProcessor
{
  public static string Process(string sourceText)
  {
    var tree = CSharpSyntaxTree.ParseText(sourceText);
    var root = tree.GetRoot();

    var rewriter = new StripRewriter();
    var stripped = rewriter.Visit(root);
    var result = stripped.ToFullString();

    return PostProcess(result);
  }

  private static string PostProcess(string text)
  {
    var lines = text.Split('\n');
    var output = new List<string>(lines.Length);

    foreach (var rawLine in lines)
    {
      var line = rawLine.TrimEnd('\r').Replace("\t", "", StringComparison.Ordinal);

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
    public StripRewriter() : base(visitIntoStructuredTrivia: true) { }

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
}
