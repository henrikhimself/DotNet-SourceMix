using Hj.SourceMix.Core;

namespace Hj.SourceMix.Tui;

internal sealed class TuiWizardResult
{
  public bool Confirmed { get; init; }

  public bool Quit { get; init; }

  public string? PromptText { get; init; }

  public string? PromptKey { get; init; }

  public GlobalPreferences GlobalPreferences { get; init; } = new();

  public MixOptions? Options { get; init; }

  public IReadOnlyList<string> SelectedFiles { get; init; } = [];

  public IReadOnlySet<string> PinnedFiles { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

  public IReadOnlyList<string> SelectedSkillKeys { get; init; } = [];

  public IReadOnlySet<string> PinnedSkillKeys { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
