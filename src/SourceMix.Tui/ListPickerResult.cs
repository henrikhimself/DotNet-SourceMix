namespace Hj.SourceMix.Tui;

internal sealed record ListPickerResult<T>(
  IReadOnlyList<T> Selected,
  IReadOnlySet<string> Pinned,
  ListPickerExitReason Reason,
  ConsoleKeyInfo? OverrideKey = null)
{
  /// <summary>
  /// Gets the raw set of selection keys tracked by the picker, including keys
  /// that may not appear in the current <see cref="ListPickerPrompt{T}.Items"/>
  /// list. When unavailable, falls back to keys derived from
  /// <see cref="Selected"/>.
  /// </summary>
  public IReadOnlySet<string>? SelectedKeys { get; init; }
}
