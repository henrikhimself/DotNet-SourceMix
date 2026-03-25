using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Hj.SourceMix.Core;

public static class PreferencesManager
{
  private const string AppName = "sourcemix";
  private const int HashPrefixLength = 12;

  public static string GetConfigDirectory()
  {
    if (OperatingSystem.IsWindows())
    {
      var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

      return Path.Combine(appData, AppName);
    }

    var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

    if (!string.IsNullOrEmpty(xdgConfig))
    {
      return Path.Combine(xdgConfig, AppName);
    }

    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    return Path.Combine(home, ".config", AppName);
  }

  public static string GetPreferencesPath(string solutionDirectory)
  {
    var normalized = solutionDirectory
      .Replace('\\', '/')
      .TrimEnd('/');

    var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized.ToLowerInvariant()));
    var hashPrefix = Convert.ToHexStringLower(hashBytes)[..HashPrefixLength];

    return Path.Combine(GetConfigDirectory(), $"sourcemix-{hashPrefix}.json");
  }

  public static SolutionPreferences Load(IFileSystem fileSystem, string solutionDirectory)
  {
    var path = GetPreferencesPath(solutionDirectory);

    if (!fileSystem.File.Exists(path))
    {
      return new SolutionPreferences { SolutionPath = solutionDirectory };
    }

    var json = fileSystem.File.ReadAllText(path);
    var preferences = JsonSerializer.Deserialize(json, PreferencesJsonContext.Default.SolutionPreferences);

    return preferences ?? new SolutionPreferences { SolutionPath = solutionDirectory };
  }

  public static void Save(IFileSystem fileSystem, string solutionDirectory, SolutionPreferences preferences)
  {
    var configDir = GetConfigDirectory();

    if (!fileSystem.Directory.Exists(configDir))
    {
      fileSystem.Directory.CreateDirectory(configDir);
    }

    var path = GetPreferencesPath(solutionDirectory);
    var json = JsonSerializer.Serialize(preferences, PreferencesJsonContext.Default.SolutionPreferences);
    fileSystem.File.WriteAllText(path, json);
  }

  public static string GetGlobalPreferencesPath()
  {
    return Path.Combine(GetConfigDirectory(), "sourcemix-global.json");
  }

  public static GlobalPreferences LoadGlobal(IFileSystem fileSystem)
  {
    var path = GetGlobalPreferencesPath();

    if (!fileSystem.File.Exists(path))
    {
      return new GlobalPreferences();
    }

    var json = fileSystem.File.ReadAllText(path);
    var preferences = JsonSerializer.Deserialize(json, GlobalPreferencesJsonContext.Default.GlobalPreferences);

    return preferences ?? new GlobalPreferences();
  }

  public static void SaveGlobal(IFileSystem fileSystem, GlobalPreferences preferences)
  {
    var configDir = GetConfigDirectory();

    if (!fileSystem.Directory.Exists(configDir))
    {
      fileSystem.Directory.CreateDirectory(configDir);
    }

    var path = GetGlobalPreferencesPath();
    var json = JsonSerializer.Serialize(preferences, GlobalPreferencesJsonContext.Default.GlobalPreferences);
    fileSystem.File.WriteAllText(path, json);
  }
}
