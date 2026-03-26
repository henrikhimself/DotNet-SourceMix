using System.IO.Abstractions;

namespace Hj.SourceMix.Core;

public sealed record Skill(string Key, string FilePath);

public static class SkillScanner
{
  private const string SkillFileName = "SKILL.md";

  public static IReadOnlyList<Skill> GetAllSkills(IFileSystem fileSystem, string skillsDirectory)
  {
    if (!fileSystem.Directory.Exists(skillsDirectory))
    {
      return [];
    }

    var skills = new List<Skill>();

    foreach (var dir in fileSystem.Directory.GetDirectories(skillsDirectory))
    {
      var skillFile = fileSystem.Path.Combine(dir, SkillFileName);

      if (!fileSystem.File.Exists(skillFile))
      {
        continue;
      }

      var key = fileSystem.Path.GetFileName(dir);
      skills.Add(new Skill(key, skillFile));
    }

    skills.Sort(static (a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Key, b.Key));

    return skills;
  }
}
