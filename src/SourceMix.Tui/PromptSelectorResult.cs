using Hj.SourceMix.Core;

namespace Hj.SourceMix.Tui;

internal sealed record PromptSelectorResult(
  string? PromptText,
  string? PromptKey,
  GlobalPreferences Preferences,
  StepResult Step);
