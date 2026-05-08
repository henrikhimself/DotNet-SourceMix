namespace Hj.SourceMix.Tui;

/// <summary>
/// Describes the outcome of a <see cref="ListPickerPrompt{T}"/> Ctrl+D
/// delete handler invocation.
/// </summary>
internal enum ListPickerDeleteResult
{
  /// <summary>The current item is not deletable; the picker continues unchanged.</summary>
  NotApplicable,

  /// <summary>The user cancelled the delete confirmation; the picker continues unchanged.</summary>
  Cancelled,

  /// <summary>The item was removed; the picker rebuilds its visible list.</summary>
  Removed,
}
