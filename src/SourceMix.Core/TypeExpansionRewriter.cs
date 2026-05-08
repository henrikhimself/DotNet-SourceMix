using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hj.SourceMix.Core;

internal sealed class TypeExpansionRewriter : CSharpSyntaxRewriter
{
  private static readonly SymbolDisplayFormat _displayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
    .WithMiscellaneousOptions(
      SymbolDisplayFormat.MinimallyQualifiedFormat.MiscellaneousOptions
      | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
      | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

  private readonly SemanticModel _semanticModel;

  public TypeExpansionRewriter(SemanticModel semanticModel)
  {
    _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
  }

  public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
  {
    ArgumentNullException.ThrowIfNull(node);

    var rewritten = (LocalDeclarationStatementSyntax)base.VisitLocalDeclarationStatement(node)!;
    if (!TryGetVarIdentifier(node.Declaration.Type, out var identifierName) || node.Declaration.Variables.Count != 1)
    {
      return rewritten;
    }

    var declarator = node.Declaration.Variables[0];
    if (declarator.Initializer is null)
    {
      return rewritten;
    }

    var type = (_semanticModel.GetDeclaredSymbol(declarator) as ILocalSymbol)?.Type;
    if (!IsExpandableType(type))
    {
      return rewritten;
    }

    var replacementType = BuildTypeSyntax(type!, identifierName.SpanStart).WithTriviaFrom(rewritten.Declaration.Type);

    return rewritten.WithDeclaration(rewritten.Declaration.WithType(replacementType));
  }

  public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
  {
    ArgumentNullException.ThrowIfNull(node);

    var rewritten = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;
    if (!TryGetVarIdentifier(node.Type, out var identifierName))
    {
      return rewritten;
    }

    var foreachInfo = _semanticModel.GetForEachStatementInfo(node);
    if (!IsExpandableType(foreachInfo.ElementType))
    {
      return rewritten;
    }

    var replacementType = BuildTypeSyntax(foreachInfo.ElementType!, identifierName.SpanStart).WithTriviaFrom(rewritten.Type);

    return rewritten.WithType(replacementType);
  }

  public override SyntaxNode? VisitDeclarationExpression(DeclarationExpressionSyntax node)
  {
    ArgumentNullException.ThrowIfNull(node);

    return base.VisitDeclarationExpression(node);
  }

  public override SyntaxNode? VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
  {
    ArgumentNullException.ThrowIfNull(node);

    var rewritten = (ImplicitObjectCreationExpressionSyntax)base.VisitImplicitObjectCreationExpression(node)!;
    var type = GetInferredType(node);
    if (!IsExpandableType(type))
    {
      return rewritten;
    }

    var replacementType = BuildTypeSyntax(type!, node.SpanStart);
    var replacement = SyntaxFactory.ObjectCreationExpression(
      replacementType,
      rewritten.ArgumentList,
      rewritten.Initializer);

    return replacement
      .WithNewKeyword(rewritten.NewKeyword.WithTrailingTrivia(SyntaxFactory.Space))
      .WithTriviaFrom(rewritten);
  }

  private static bool ContainsAnonymousType(ITypeSymbol symbol)
  {
    ArgumentNullException.ThrowIfNull(symbol);

    if (symbol.IsAnonymousType)
    {
      return true;
    }

    return symbol switch
    {
      IArrayTypeSymbol arrayType => ContainsAnonymousType(arrayType.ElementType),
      IPointerTypeSymbol pointerType => ContainsAnonymousType(pointerType.PointedAtType),
      INamedTypeSymbol namedType when namedType.IsTupleType => namedType.TupleElements.Any(e => ContainsAnonymousType(e.Type)),
      INamedTypeSymbol namedType when namedType.IsGenericType => namedType.TypeArguments.Any(ContainsAnonymousType),
      _ => false,
    };
  }

  private static bool IsExpandableType(ITypeSymbol? symbol)
  {
    return symbol is not null
      && symbol is not IErrorTypeSymbol
      && symbol.TypeKind != TypeKind.Dynamic
      && symbol.SpecialType != SpecialType.System_Void
      && !ContainsAnonymousType(symbol);
  }

  private TypeSyntax BuildTypeSyntax(ITypeSymbol symbol, int position)
  {
    var display = symbol.ToMinimalDisplayString(_semanticModel, position, _displayFormat);

    return SyntaxFactory.ParseTypeName(display);
  }

  private ITypeSymbol? GetInferredType(ExpressionSyntax expression)
  {
    var typeInfo = _semanticModel.GetTypeInfo(expression);

    return typeInfo.Type ?? typeInfo.ConvertedType;
  }

  private bool TryGetVarIdentifier(TypeSyntax typeSyntax, out IdentifierNameSyntax identifierName)
  {
    if (typeSyntax is IdentifierNameSyntax candidate
        && string.Equals(candidate.Identifier.ValueText, "var", StringComparison.Ordinal)
        && candidate.SyntaxTree == _semanticModel.SyntaxTree)
    {
      identifierName = candidate;
      return true;
    }

    identifierName = null!;
    return false;
  }
}
