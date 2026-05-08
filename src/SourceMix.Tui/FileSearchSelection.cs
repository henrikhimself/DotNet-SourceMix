namespace Hj.SourceMix.Tui;

internal sealed record FileSearchSelection(IReadOnlyList<string> SelectedPaths, IReadOnlySet<string> PinnedPaths);
