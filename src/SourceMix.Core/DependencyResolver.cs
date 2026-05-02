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
    // Phase 1 — File discovery
    var allCsFiles = fileSystem.Directory
      .EnumerateFiles(solutionDirectory, "*.cs", SearchOption.AllDirectories)
      .Select(fileSystem.Path.GetFullPath)
      .Where(f => !IsExcluded(f))
      .ToList();

    // Phase 2 — Type index (parse → extract → discard tree)
    var typeToFiles = BuildTypeToFileIndex(fileSystem, allCsFiles);

    // Phase 3 — BFS traversal (lazy parse with cache)
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

    var syntaxCache = new Dictionary<string, SyntaxTree>(StringComparer.OrdinalIgnoreCase);

    for (var depth = 0; depth < maxDepth && currentLevel.Count > 0; depth++)
    {
      var nextLevel = new List<string>();

      foreach (var filePath in currentLevel)
      {
        if (!syntaxCache.TryGetValue(filePath, out var tree))
        {
          var source = fileSystem.File.ReadAllText(filePath);
          tree = CSharpSyntaxTree.ParseText(source, path: filePath);
          syntaxCache[filePath] = tree;
        }

        var referencedNames = ExtractReferencedTypeNames(tree);
        var usingNamespaces = ExtractUsingNamespaces(tree);

        foreach (var name in referencedNames)
        {
          if (!typeToFiles.TryGetValue(name, out var candidates))
          {
            unresolvedTypeNames.Add(name);
            continue;
          }

          var resolvedFiles = candidates.Count > 1
            ? NarrowByNamespace(candidates, usingNamespaces)
            : candidates;

          foreach (var (definingFile, _) in resolvedFiles)
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

  private static Dictionary<string, List<(string FilePath, string? Namespace)>> BuildTypeToFileIndex(
    IFileSystem fileSystem,
    IEnumerable<string> filePaths)
  {
    var map = new Dictionary<string, List<(string FilePath, string? Namespace)>>(StringComparer.Ordinal);

    foreach (var filePath in filePaths)
    {
      var source = fileSystem.File.ReadAllText(filePath);
      var tree = CSharpSyntaxTree.ParseText(source, path: filePath);
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

        var ns = GetEnclosingNamespace(declaration);

        if (!map.TryGetValue(name, out var list))
        {
          list = [];
          map[name] = list;
        }

        if (!list.Any(e => string.Equals(e.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
        {
          list.Add((filePath, ns));
        }
      }
    }

    return map;
  }

  private static List<(string FilePath, string? Namespace)> NarrowByNamespace(
    List<(string FilePath, string? Namespace)> candidates,
    HashSet<string> usingNamespaces)
  {
    var narrowed = candidates
      .Where(c => c.Namespace is not null && usingNamespaces.Contains(c.Namespace))
      .ToList();

    return narrowed.Count > 0 ? narrowed : candidates;
  }

  private static string? GetEnclosingNamespace(SyntaxNode node)
  {
    var parent = node.Parent;

    while (parent is not null)
    {
      if (parent is NamespaceDeclarationSyntax ns)
      {
        return ns.Name.ToString();
      }

      if (parent is FileScopedNamespaceDeclarationSyntax fsns)
      {
        return fsns.Name.ToString();
      }

      parent = parent.Parent;
    }

    return null;
  }

  private static HashSet<string> ExtractUsingNamespaces(SyntaxTree tree)
  {
    var namespaces = new HashSet<string>(StringComparer.Ordinal);
    var root = tree.GetRoot();

    foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
    {
      if (!usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && usingDirective.Alias is null)
      {
        namespaces.Add(usingDirective.NamespaceOrType.ToString());
      }
    }

    return namespaces;
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

  private static bool IsExcluded(string fullPath)
  {
    var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    foreach (var part in parts)
    {
      if (part.Equals("bin", StringComparison.OrdinalIgnoreCase)
        || part.Equals("obj", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }
}
