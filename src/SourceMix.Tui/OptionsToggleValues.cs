namespace Hj.SourceMix.Tui;

internal sealed record OptionsToggleValues(
  bool Recursive,
  int MaxDepth,
  bool IncludeCompiled,
  bool Trim);
