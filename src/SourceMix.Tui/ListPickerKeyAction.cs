namespace Hj.SourceMix.Tui;

internal enum ListPickerKeyAction
{
  /// <summary>The override hook did not handle the key; default handling proceeds.</summary>
  NotHandled,

  /// <summary>The override hook consumed the key; the picker continues running.</summary>
  Handled,

  /// <summary>The override hook requests the picker to exit with reason <see cref="ListPickerExitReason.KeyOverride"/>.</summary>
  Exit,
}
