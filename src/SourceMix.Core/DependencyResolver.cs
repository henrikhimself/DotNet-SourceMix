using System.IO.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hj.SourceMix.Core;

public static class DependencyResolver
{
  public static DependencyResult Resolve(
    IFileSystem fileSystem,
    IReadOnlyList<string> seedFiles,
    string solutionDirectory,
    int maxDepth)
  {
    var allCsFiles = fileSystem.Directory.GetFiles(solutionDirectory, "*.cs", SearchOption.AllDirectories);

    var fileIndex = new Dictionary<string, SyntaxTree>(StringComparer.OrdinalIgnoreCase);
    foreach (var file in allCsFiles)
    {
      var fullPath = fileSystem.Path.GetFullPath(file);
      var source = fileSystem.File.ReadAllText(fullPath);
      var tree = CSharpSyntaxTree.ParseText(source, path: fullPath);
      fileIndex[fullPath] = tree;
    }

    var typeToFiles = BuildTypeToFileMap(fileIndex);

    var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var unresolvedTypeNames = new HashSet<string>(StringComparer.Ordinal);
    var currentLevel = new List<string>();

    foreach (var seed in seedFiles)
    {
      var fullPath = fileSystem.Path.GetFullPath(seed);

      if (included.Add(fullPath))
      {
        currentLevel.Add(fullPath);
      }
    }

    for (var depth = 0; depth < maxDepth && currentLevel.Count > 0; depth++)
    {
      var nextLevel = new List<string>();

      foreach (var filePath in currentLevel)
      {
        if (!fileIndex.TryGetValue(filePath, out var tree))
        {
          continue;
        }

        var referencedNames = ExtractReferencedTypeNames(tree);

        foreach (var name in referencedNames)
        {
          if (!typeToFiles.TryGetValue(name, out var definingFiles))
          {
            unresolvedTypeNames.Add(name);
            continue;
          }

          foreach (var definingFile in definingFiles)
          {
            if (included.Add(definingFile))
            {
              nextLevel.Add(definingFile);
            }
          }
        }
      }

      currentLevel = nextLevel;
    }

    var files = seedFiles
      .Concat(included.Except(seedFiles, StringComparer.OrdinalIgnoreCase))
      .Where(included.Contains)
      .ToList();

    return new DependencyResult(files, unresolvedTypeNames);
  }

  private static Dictionary<string, List<string>> BuildTypeToFileMap(
    Dictionary<string, SyntaxTree> fileIndex)
  {
    var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);

    foreach (var (filePath, tree) in fileIndex)
    {
      var root = tree.GetRoot();

      foreach (var declaration in root.DescendantNodes())
      {
        var name = declaration switch
        {
          ClassDeclarationSyntax c => c.Identifier.Text,
          StructDeclarationSyntax s => s.Identifier.Text,
          InterfaceDeclarationSyntax i => i.Identifier.Text,
          EnumDeclarationSyntax e => e.Identifier.Text,
          RecordDeclarationSyntax r => r.Identifier.Text,
          DelegateDeclarationSyntax d => d.Identifier.Text,
          _ => null,
        };

        if (name is null)
        {
          continue;
        }

        if (!map.TryGetValue(name, out var list))
        {
          list = [];
          map[name] = list;
        }

        if (!list.Contains(filePath, StringComparer.OrdinalIgnoreCase))
        {
          list.Add(filePath);
        }
      }
    }

    return map;
  }

  private static HashSet<string> ExtractReferencedTypeNames(SyntaxTree tree)
  {
    var names = new HashSet<string>(StringComparer.Ordinal);
    var root = tree.GetRoot();

    foreach (var node in root.DescendantNodes())
    {
      switch (node)
      {
        case IdentifierNameSyntax identifier:
          AddIfTypeLikeName(names, identifier.Identifier.Text);
          break;

        case GenericNameSyntax generic:
          AddIfTypeLikeName(names, generic.Identifier.Text);
          break;

        case BaseTypeSyntax baseType when baseType.Type is IdentifierNameSyntax baseName:
          names.Add(baseName.Identifier.Text);
          break;

        case BaseTypeSyntax baseType when baseType.Type is GenericNameSyntax baseGeneric:
          names.Add(baseGeneric.Identifier.Text);
          break;

        case ObjectCreationExpressionSyntax creation when creation.Type is IdentifierNameSyntax ctorName:
          names.Add(ctorName.Identifier.Text);
          break;

        case ObjectCreationExpressionSyntax creation when creation.Type is GenericNameSyntax ctorGeneric:
          names.Add(ctorGeneric.Identifier.Text);
          break;
      }
    }

    return names;
  }

  private static void AddIfTypeLikeName(HashSet<string> names, string name)
  {
    if (name.Length > 0 && char.IsUpper(name[0]))
    {
      names.Add(name);
    }
  }
}

public sealed record DependencyResult(
  IReadOnlyList<string> Files,
  IReadOnlySet<string> UnresolvedTypeNames);
