using System.IO.Abstractions;
using Hj.SourceMix.Core;
using Spectre.Console;

namespace Hj.SourceMix.Tui;

/// <summary>
/// Drives the interactive wizard as an explicit state machine: FilesStep ->
/// OptionsStep -> OutputStep -> PromptStep -> SkillsStep -> Run. Pressing
/// Ctrl+Q on any step returns to the previous step (or exits when on the
/// first step). All user choices made on earlier steps are preserved when
/// re-entering a step. The screen is cleared on every step transition so each
/// step renders on a fresh canvas.
/// </summary>
internal static class TuiWizard
{
  public static TuiWizardResult Run(
    GlobalPreferences globalPreferences,
    SolutionPreferences preferences,
    IReadOnlyList<CsFile> files,
    IReadOnlySet<string> initiallyPinnedFiles,
    IReadOnlyList<Skill> skills,
    IReadOnlySet<string> initiallyPinnedSkills,
    ITuiConsole console,
    IKeyReader keys,
    IFileSystem? fileSystem = null)
  {
    ArgumentNullException.ThrowIfNull(globalPreferences);
    ArgumentNullException.ThrowIfNull(preferences);
    ArgumentNullException.ThrowIfNull(files);
    ArgumentNullException.ThrowIfNull(initiallyPinnedFiles);
    ArgumentNullException.ThrowIfNull(skills);
    ArgumentNullException.ThrowIfNull(initiallyPinnedSkills);
    ArgumentNullException.ThrowIfNull(console);
    ArgumentNullException.ThrowIfNull(keys);

    var fs = fileSystem ?? new FileSystem();

    var state = new WizardState
    {
      GlobalPreferences = globalPreferences,
      PinnedFiles = new HashSet<string>(initiallyPinnedFiles, StringComparer.OrdinalIgnoreCase),
      SelectedFiles = new HashSet<string>(initiallyPinnedFiles, StringComparer.OrdinalIgnoreCase),
      PinnedSkills = new HashSet<string>(initiallyPinnedSkills, StringComparer.OrdinalIgnoreCase),
      SelectedSkills = new HashSet<string>(initiallyPinnedSkills, StringComparer.OrdinalIgnoreCase),
    };

    var step = WizardStep.Files;

    while (step != WizardStep.Exit && step != WizardStep.Run && step != WizardStep.Quit)
    {
      console.ClearScreen();
      TuiRender.WriteAppHeading(console);

      step = step switch
      {
        WizardStep.Files => RunFilesStep(state, files, console, keys),
        WizardStep.Options => RunOptionsStep(state, preferences, console, keys),
        WizardStep.Output => RunOutputStep(state, preferences, console, keys, fs),
        WizardStep.Prompt => RunPromptStep(state, preferences, skills, console, keys),
        WizardStep.Skills => RunSkillsStep(state, skills, console, keys),
        _ => WizardStep.Exit,
      };
    }

    if (step == WizardStep.Quit)
    {
      return new TuiWizardResult
      {
        Confirmed = false,
        Quit = true,
        GlobalPreferences = state.GlobalPreferences,
      };
    }

    if (step == WizardStep.Exit)
    {
      return new TuiWizardResult
      {
        Confirmed = false,
        GlobalPreferences = state.GlobalPreferences,
      };
    }

    var values = state.Options!;
    var outputPath = state.OutputPath!;
    var options = new MixOptions(
      values.Recursive,
      values.MaxDepth,
      values.IncludeCompiled,
      values.Trim,
      outputPath);

    return new TuiWizardResult
    {
      Confirmed = true,
      PromptText = state.PromptText,
      PromptKey = state.PromptKey,
      GlobalPreferences = state.GlobalPreferences,
      Options = options,
      SelectedFiles = [.. state.SelectedFiles],
      PinnedFiles = state.PinnedFiles,
      SelectedSkillKeys = [.. state.SelectedSkills],
      PinnedSkillKeys = state.PinnedSkills,
    };
  }

  private static WizardStep RunPromptStep(
    WizardState state,
    SolutionPreferences preferences,
    IReadOnlyList<Skill> skills,
    ITuiConsole console,
    IKeyReader keys)
  {
    var result = PromptSelector.Show(state.GlobalPreferences, preferences.DefaultPromptKey, console, keys);

    if (result.Step == StepResult.Quit)
    {
      return WizardStep.Quit;
    }

    if (result.Step == StepResult.Back)
    {
      return WizardStep.Output;
    }

    state.PromptText = result.PromptText;
    state.PromptKey = result.PromptKey;
    state.GlobalPreferences = result.Preferences;

    return skills.Count > 0 ? WizardStep.Skills : WizardStep.Run;
  }

  private static WizardStep RunOptionsStep(
    WizardState state,
    SolutionPreferences preferences,
    ITuiConsole console,
    IKeyReader keys)
  {
    var seedPreferences = state.Options is null
      ? preferences
      : preferences with
      {
        Defaults = preferences.Defaults with
        {
          Recursive = state.Options.Recursive,
          LimitDepth = state.Options.MaxDepth != int.MaxValue,
          MaxDepth = state.Options.MaxDepth == int.MaxValue ? preferences.Defaults.MaxDepth : state.Options.MaxDepth,
          IncludeCompiled = state.Options.IncludeCompiled,
          Trim = state.Options.Trim,
        },
      };

    var result = OptionsPrompt.Show(seedPreferences, console, keys);

    if (result.Step == StepResult.Quit)
    {
      return WizardStep.Quit;
    }

    if (result.Step == StepResult.Back)
    {
      return WizardStep.Files;
    }

    state.Options = result.Values;

    return WizardStep.Output;
  }

  private static WizardStep RunOutputStep(
    WizardState state,
    SolutionPreferences preferences,
    ITuiConsole console,
    IKeyReader keys,
    IFileSystem fileSystem)
  {
    var initialPath = state.OutputPath
      ?? preferences.OutputPath
      ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "sourcemix.md");

    var result = OutputPathPrompt.Show(initialPath, console, keys, fileSystem);

    if (result.Step == StepResult.Quit)
    {
      return WizardStep.Quit;
    }

    if (result.Step == StepResult.Back)
    {
      return WizardStep.Options;
    }

    state.OutputPath = result.Path;

    return WizardStep.Prompt;
  }

  private static WizardStep RunFilesStep(
    WizardState state,
    IReadOnlyList<CsFile> files,
    ITuiConsole console,
    IKeyReader keys)
  {
    if (files.Count == 0)
    {
      console.Ansi.MarkupLine("[yellow]No .cs files found in the solution directory.[/]");
      return WizardStep.Exit;
    }

    var showZeroFilesHint = false;

    while (true)
    {
      if (showZeroFilesHint)
      {
        console.Ansi.MarkupLine("[yellow]Select at least one file (Ctrl+Q to go back, Esc to quit).[/]");
      }

      var selection = FileSearchPrompt.Show(files, state.PinnedFiles, console, keys);

      if (selection.Step == StepResult.Quit)
      {
        return WizardStep.Quit;
      }

      if (selection.Step == StepResult.Back)
      {
        return WizardStep.Exit;
      }

      if (selection.SelectedPaths.Count == 0)
      {
        showZeroFilesHint = true;
        continue;
      }

      state.SelectedFiles = new HashSet<string>(selection.SelectedPaths, StringComparer.OrdinalIgnoreCase);
      state.PinnedFiles = new HashSet<string>(selection.PinnedPaths, StringComparer.OrdinalIgnoreCase);

      return WizardStep.Options;
    }
  }

  private static WizardStep RunSkillsStep(
    WizardState state,
    IReadOnlyList<Skill> skills,
    ITuiConsole console,
    IKeyReader keys)
  {
    var selection = SkillSelectionPrompt.Show(skills, state.PinnedSkills, console, keys);

    if (selection.Step == StepResult.Quit)
    {
      return WizardStep.Quit;
    }

    if (selection.Step == StepResult.Back)
    {
      return WizardStep.Prompt;
    }

    state.SelectedSkills = new HashSet<string>(selection.SelectedKeys, StringComparer.OrdinalIgnoreCase);
    state.PinnedSkills = new HashSet<string>(selection.PinnedKeys, StringComparer.OrdinalIgnoreCase);

    return WizardStep.Run;
  }

  private sealed class WizardState
  {
    public string? PromptText { get; set; }

    public string? PromptKey { get; set; }

    public GlobalPreferences GlobalPreferences { get; set; } = new();

    public OptionsToggleValues? Options { get; set; }

    public string? OutputPath { get; set; }

    public HashSet<string> SelectedFiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> PinnedFiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> SelectedSkills { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> PinnedSkills { get; set; } = new(StringComparer.OrdinalIgnoreCase);
  }
}
