#pragma warning disable CA1861 // CA1861 noisy in xUnit Assert.Equal calls.
using Hj.SourceMix.Core;

namespace Hj.SourceMix.Tui.Tests;

public sealed class SkillSelectionPromptTests
{
  [Fact]
  public void SelectThenConfirm_ReturnsSelectedKeys()
  {
    var skills = new[]
    {
      new Skill("alpha", "alpha.skill.md"),
      new Skill("beta", "beta.skill.md"),
      new Skill("gamma", "gamma.skill.md"),
    };
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader(
    [
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.DownArrow),
      K(ConsoleKey.Spacebar, ' '),
      K(ConsoleKey.Enter),
    ]);

    var pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var result = SkillSelectionPrompt.Show(skills, pinned, console, keys);

    Assert.Equal(new[] { "alpha", "gamma" }, result.SelectedKeys);
    Assert.Empty(result.PinnedKeys);
  }

  [Fact]
  public void Esc_ReturnsEmptySelection()
  {
    var skills = new[]
    {
      new Skill("alpha", "alpha.skill.md"),
      new Skill("beta", "beta.skill.md"),
    };
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.Escape)]);

    var pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha" };
    var result = SkillSelectionPrompt.Show(skills, pinned, console, keys);

    Assert.Empty(result.SelectedKeys);
    Assert.Empty(result.PinnedKeys);
  }

  [Fact]
  public void InitialPinned_AreSelectedByDefault()
  {
    var skills = new[]
    {
      new Skill("alpha", "alpha.skill.md"),
      new Skill("beta", "beta.skill.md"),
    };
    using var console = new TestTuiConsole();
    var keys = new FakeKeyReader([K(ConsoleKey.Enter)]);

    var pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "beta" };
    var result = SkillSelectionPrompt.Show(skills, pinned, console, keys);

    Assert.Equal(new[] { "beta" }, result.SelectedKeys);
    Assert.Contains("beta", result.PinnedKeys);
  }

  private static ConsoleKeyInfo K(ConsoleKey key, char ch = '\0', bool ctrl = false)
    => new(ch, key, shift: false, alt: false, control: ctrl);
}
