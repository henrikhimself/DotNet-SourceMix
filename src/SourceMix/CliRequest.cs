namespace Hj.SourceMix;

internal sealed record CliRequest(
  string[] Files,
  FileInfo? Output,
  bool Recursive,
  int Depth,
  bool IncludeCompiled,
  bool Trim,
  string? Prompt,
  string[] SkillKeys);
