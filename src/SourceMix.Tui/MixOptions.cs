namespace Hj.SourceMix.Tui;

internal sealed record MixOptions(
  bool Recursive,
  int MaxDepth,
  bool IncludeCompiled,
  bool Trim,
  bool ExpandTypes,
  string OutputPath);
