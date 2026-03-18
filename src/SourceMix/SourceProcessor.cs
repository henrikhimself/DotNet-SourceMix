using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hj.SourceMix;

internal static class SourceProcessor
{
  internal static string Process(string sourceText)
  {
    var tree = CSharpSyntaxTree.ParseText(sourceText);
    var root = tree.GetRoot();

    var rewriter = new StripRewriter();
    var stripped = rewriter.Visit(root);
    var result = stripped.ToFullString();

    return CollapseBlankLines(result);
  }

  private static string CollapseBlankLines(string text)
  {
    var lines = text.Split('\n');
    var output = new List<string>(lines.Length);
    var consecutiveBlank = 0;

    foreach (var rawLine in lines)
    {
      var line = rawLine.TrimEnd('\r');

      if (string.IsNullOrWhiteSpace(line))
      {
        consecutiveBlank++;

        if (consecutiveBlank <= 1)
        {
          output.Add(line);
        }
      }
      else
      {
        consecutiveBlank = 0;
        output.Add(line);
      }
    }

    return string.Join("\n", output).TrimStart('\n').TrimEnd('\n', ' ', '\r');
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
