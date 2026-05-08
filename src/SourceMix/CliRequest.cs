namespace Hj.SourceMix;

internal sealed record CliRequest(
  string[] Files,
  FileInfo? Output,
  bool Recursive,
  int Depth,
  bool IncludeCompiled,
  bool Trim,
  bool ExpandTypes,
  string? Prompt,
  string[] SkillKeys);
