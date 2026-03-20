using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace Hj.SourceMix.Core;

public static class AssemblyDecompiler
{
  public static IReadOnlyList<string> Decompile(
    IReadOnlySet<string> typeNames,
    string solutionDirectory)
  {
    var assemblyPaths = FindAssemblies(solutionDirectory);

    if (assemblyPaths.Count == 0)
    {
      return [];
    }

    var results = new List<string>();

    foreach (var assemblyPath in assemblyPaths)
    {
      try
      {
        var decompiled = DecompileFromAssembly(typeNames, assemblyPath);
        results.AddRange(decompiled);
      }
      catch (Exception)
      {
        // Skip assemblies that cannot be loaded or decompiled.
      }
    }

    return results;
  }

  private static List<string> FindAssemblies(string solutionDirectory)
  {
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var assemblies = new List<string>();

    foreach (var binDir in Directory.EnumerateDirectories(solutionDirectory, "bin", SearchOption.AllDirectories))
    {
      foreach (var dll in Directory.EnumerateFiles(binDir, "*.dll", SearchOption.AllDirectories))
      {
        var fullPath = Path.GetFullPath(dll);

        if (seen.Add(fullPath))
        {
          assemblies.Add(fullPath);
        }
      }
    }

    return assemblies;
  }

  private static List<string> DecompileFromAssembly(
    IReadOnlySet<string> typeNames,
    string assemblyPath)
  {
    var settings = new DecompilerSettings
    {
      ThrowOnAssemblyResolveErrors = false,
    };

    var decompiler = new CSharpDecompiler(assemblyPath, settings);
    var results = new List<string>();

    foreach (var typeDef in decompiler.TypeSystem.MainModule.TypeDefinitions)
    {
      if (!typeNames.Contains(typeDef.Name))
      {
        continue;
      }

      if (!IsDecompilable(typeDef))
      {
        continue;
      }

      try
      {
        var source = decompiler.DecompileTypeAsString(typeDef.FullTypeName);
        var assemblyName = Path.GetFileName(assemblyPath);
        results.Add($"// Decompiled from {assemblyName}\n{source}");
      }
      catch (Exception)
      {
        // Skip types that cannot be decompiled.
      }
    }

    return results;
  }

  private static bool IsDecompilable(ITypeDefinition typeDef)
  {
    return typeDef.Kind switch
    {
      TypeKind.Interface => true,
      TypeKind.Class => IsSimpleModel(typeDef),
      TypeKind.Struct => IsSimpleModel(typeDef),
      _ => false,
    };
  }

  private static bool IsSimpleModel(ITypeDefinition typeDef)
  {
    if (typeDef.IsAbstract || typeDef.IsStatic)
    {
      return false;
    }

    var members = typeDef.Members.ToList();

    if (members.Count == 0)
    {
      return false;
    }

    var properties = members.OfType<IProperty>().ToList();
    var methods = members.OfType<IMethod>()
      .Where(m => !m.IsConstructor && !m.IsDestructor)
      .ToList();

    var allowedMethodNames = new HashSet<string>(StringComparer.Ordinal)
    {
      "ToString",
      "Equals",
      "GetHashCode",
      "op_Equality",
      "op_Inequality",
    };

    var hasOnlyAllowedMethods = methods.All(m => allowedMethodNames.Contains(m.Name));

    if (!hasOnlyAllowedMethods)
    {
      return false;
    }

    if (properties.Count == 0)
    {
      return false;
    }

    var autoPropertyCount = properties.Count(IsAutoProperty);
    var ratio = (double)autoPropertyCount / properties.Count;

    return ratio >= 0.5;
  }

  private static bool IsAutoProperty(IProperty property)
  {
    return (property.CanGet || property.CanSet)
      && property.Getter?.HasBody != true
      && property.Setter?.HasBody != true;
  }
}
