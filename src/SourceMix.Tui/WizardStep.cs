namespace Hj.SourceMix.Tui;

internal enum WizardStep
{
  /// <summary>Pick a prompt personality.</summary>
  Prompt,

  /// <summary>Configure mix options (toggles and recursion depth).</summary>
  Options,

  /// <summary>Choose the output file path.</summary>
  Output,

  /// <summary>Pick the seed source files.</summary>
  Files,

  /// <summary>Pick which skills to include.</summary>
  Skills,

  /// <summary>All steps confirmed; execute the mix.</summary>
  Run,

  /// <summary>The wizard was aborted before completion.</summary>
  Exit,

  /// <summary>The user requested an immediate quit (Esc).</summary>
  Quit,
}
