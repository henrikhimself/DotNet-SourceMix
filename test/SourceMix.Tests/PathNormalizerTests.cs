namespace Hj.SourceMix.Tests;

public sealed class PathNormalizerTests
{
  [Fact]
  public void Normalize_ResolvesRelativeSegments()
  {
    var input = Path.Combine(Path.GetTempPath(), "a", "..", "b", "file.cs");
    var expected = Path.Combine(Path.GetTempPath(), "b", "file.cs");

    Assert.Equal(expected, PathNormalizer.Normalize(input));
  }

  [Fact]
  public void Normalize_TrimsTrailingDirectorySeparator()
  {
    var dir = Path.Combine(Path.GetTempPath(), "trailing");
    var withSep = dir + Path.DirectorySeparatorChar;

    Assert.Equal(PathNormalizer.Normalize(dir), PathNormalizer.Normalize(withSep));
  }

  [Fact]
  public void Normalize_PreservesRootSeparator()
  {
    var root = Path.GetPathRoot(Path.GetTempPath()) ?? string.Empty;

    if (string.IsNullOrEmpty(root))
    {
      return;
    }

    var normalized = PathNormalizer.Normalize(root);

    Assert.NotEmpty(normalized);
    Assert.EndsWith(Path.DirectorySeparatorChar.ToString(), normalized, StringComparison.Ordinal);
  }

  [Fact]
  public void Comparer_MatchesPlatformConvention()
  {
    if (OperatingSystem.IsLinux())
    {
      Assert.Same(StringComparer.Ordinal, PathNormalizer.Comparer);
    }
    else
    {
      Assert.Same(StringComparer.OrdinalIgnoreCase, PathNormalizer.Comparer);
    }
  }

  [Fact]
  public void Comparer_TreatsSamePathAsEqual()
  {
    var a = PathNormalizer.Normalize(Path.Combine(Path.GetTempPath(), "Foo", "..", "Bar", "x.cs"));
    var b = PathNormalizer.Normalize(Path.Combine(Path.GetTempPath(), "Bar", "x.cs"));

    Assert.True(PathNormalizer.Comparer.Equals(a, b));
  }
}
