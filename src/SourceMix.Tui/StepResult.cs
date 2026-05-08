namespace Hj.SourceMix.Tui;

internal enum StepResult
{
  /// <summary>The user confirmed the step (Enter).</summary>
  Confirm,

  /// <summary>The user requested to go back to the previous step (Ctrl+Q).</summary>
  Back,

  /// <summary>The user requested to quit the wizard immediately (Esc).</summary>
  Quit,
}
