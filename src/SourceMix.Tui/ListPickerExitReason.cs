namespace Hj.SourceMix.Tui;

internal enum ListPickerExitReason
{
  /// <summary>The user confirmed the selection (Enter).</summary>
  Confirmed,

  /// <summary>The user requested to go back (Ctrl+Q).</summary>
  Skipped,

  /// <summary>The user pressed a key handled by the <see cref="ListPickerPrompt{T}.KeyOverride"/> hook with an Exit action.</summary>
  KeyOverride,

  /// <summary>The user requested an immediate quit (Esc).</summary>
  Quit,
}
