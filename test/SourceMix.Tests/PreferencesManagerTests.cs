using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;

namespace Hj.SourceMix.Tests;

public sealed class PreferencesManagerTests
{
  [Fact]
  public void Load_NoFile_ReturnsDefaultPreferences()
  {
    var fs = new MockFileSystem();
    var solutionDir = fs.Path.Combine(fs.Path.GetTempPath(), "MySolution");
    fs.Directory.CreateDirectory(solutionDir);

    var result = PreferencesManager.Load(fs, solutionDir);

    Assert.Equal(solutionDir, result.SolutionPath);
    Assert.Empty(result.PinnedFiles);
    Assert.Null(result.OutputPath);
    Assert.False(result.Defaults.Recursive);
    Assert.False(result.Defaults.LimitDepth);
    Assert.Equal(3, result.Defaults.MaxDepth);
    Assert.False(result.Defaults.IncludeCompiled);
  }

  [Fact]
  public void SaveAndLoad_RoundTrip_PreservesAllValues()
  {
    var fs = new MockFileSystem();
    var solutionDir = fs.Path.Combine(fs.Path.GetTempPath(), "MySolution");
    fs.Directory.CreateDirectory(solutionDir);

    var preferences = new SolutionPreferences
    {
      SolutionPath = solutionDir,
      PinnedFiles = ["src/Foo.cs", "src/Bar.cs"],
      OutputPath = "/tmp/output.md",
      Defaults = new PreferenceDefaults
      {
        Recursive = true,
        LimitDepth = true,
        MaxDepth = 5,
        IncludeCompiled = true,
      },
    };

    PreferencesManager.Save(fs, solutionDir, preferences);
    var loaded = PreferencesManager.Load(fs, solutionDir);

    Assert.Equal(solutionDir, loaded.SolutionPath);
    Assert.Equal(["src/Foo.cs", "src/Bar.cs"], loaded.PinnedFiles);
    Assert.Equal("/tmp/output.md", loaded.OutputPath);
    Assert.True(loaded.Defaults.Recursive);
    Assert.True(loaded.Defaults.LimitDepth);
    Assert.Equal(5, loaded.Defaults.MaxDepth);
    Assert.True(loaded.Defaults.IncludeCompiled);
  }

  [Fact]
  public void Save_CreatesConfigDirectory()
  {
    var fs = new MockFileSystem();
    var solutionDir = fs.Path.Combine(fs.Path.GetTempPath(), "MySolution");
    fs.Directory.CreateDirectory(solutionDir);

    var preferences = new SolutionPreferences { SolutionPath = solutionDir };

    PreferencesManager.Save(fs, solutionDir, preferences);

    var configDir = PreferencesManager.GetConfigDirectory();
    Assert.True(fs.Directory.Exists(configDir));
  }

  [Fact]
  public void Save_WritesValidJson()
  {
    var fs = new MockFileSystem();
    var solutionDir = fs.Path.Combine(fs.Path.GetTempPath(), "MySolution");
    fs.Directory.CreateDirectory(solutionDir);

    var preferences = new SolutionPreferences
    {
      SolutionPath = solutionDir,
      PinnedFiles = ["src/Foo.cs"],
    };

    PreferencesManager.Save(fs, solutionDir, preferences);

    var path = PreferencesManager.GetPreferencesPath(solutionDir);
    var json = fs.File.ReadAllText(path);
    var document = JsonDocument.Parse(json);

    Assert.Equal(solutionDir, document.RootElement.GetProperty("solutionPath").GetString());
    Assert.Single(document.RootElement.GetProperty("pinnedFiles").EnumerateArray());
  }

  [Fact]
  public void GetPreferencesPath_SamePath_ReturnsSameHash()
  {
    var path1 = PreferencesManager.GetPreferencesPath("/Users/test/MySolution");
    var path2 = PreferencesManager.GetPreferencesPath("/Users/test/MySolution");

    Assert.Equal(path1, path2);
  }

  [Fact]
  public void GetPreferencesPath_DifferentPaths_ReturnDifferentHashes()
  {
    var path1 = PreferencesManager.GetPreferencesPath("/Users/test/SolutionA");
    var path2 = PreferencesManager.GetPreferencesPath("/Users/test/SolutionB");

    Assert.NotEqual(path1, path2);
  }

  [Fact]
  public void GetPreferencesPath_NormalizesSeparators()
  {
    var path1 = PreferencesManager.GetPreferencesPath("C:\\Users\\test\\MySolution");
    var path2 = PreferencesManager.GetPreferencesPath("C:/Users/test/MySolution");

    Assert.Equal(path1, path2);
  }

  [Fact]
  public void GetPreferencesPath_CaseInsensitiveHash()
  {
    var path1 = PreferencesManager.GetPreferencesPath("/Users/Test/MySolution");
    var path2 = PreferencesManager.GetPreferencesPath("/users/test/mysolution");

    Assert.Equal(path1, path2);
  }

  [Fact]
  public void GetPreferencesPath_TrailingSlashIgnored()
  {
    var path1 = PreferencesManager.GetPreferencesPath("/Users/test/MySolution/");
    var path2 = PreferencesManager.GetPreferencesPath("/Users/test/MySolution");

    Assert.Equal(path1, path2);
  }

  [Fact]
  public void Load_NullOutputPath_ReturnsNullOutputPath()
  {
    var fs = new MockFileSystem();
    var solutionDir = fs.Path.Combine(fs.Path.GetTempPath(), "MySolution");
    fs.Directory.CreateDirectory(solutionDir);

    var preferences = new SolutionPreferences
    {
      SolutionPath = solutionDir,
    };

    PreferencesManager.Save(fs, solutionDir, preferences);
    var loaded = PreferencesManager.Load(fs, solutionDir);

    Assert.Null(loaded.OutputPath);
  }

  [Fact]
  public void Save_OverwritesExistingPreferences()
  {
    var fs = new MockFileSystem();
    var solutionDir = fs.Path.Combine(fs.Path.GetTempPath(), "MySolution");
    fs.Directory.CreateDirectory(solutionDir);

    var first = new SolutionPreferences
    {
      SolutionPath = solutionDir,
      PinnedFiles = ["src/Old.cs"],
    };

    PreferencesManager.Save(fs, solutionDir, first);

    var second = new SolutionPreferences
    {
      SolutionPath = solutionDir,
      PinnedFiles = ["src/New.cs"],
    };

    PreferencesManager.Save(fs, solutionDir, second);
    var loaded = PreferencesManager.Load(fs, solutionDir);

    Assert.Equal(["src/New.cs"], loaded.PinnedFiles);
  }
}
