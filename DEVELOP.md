# Developer Notes

This file is for repository contributors. It focuses on internal structure and
change surfaces that are not already described in `README.md` or `AGENTS.md`.

## Code map

| Area | Main files | Notes |
| --- | --- | --- |
| CLI surface | `src/SourceMix/SourceMixCommandFactory.cs`, `src/SourceMix/CliRequest.cs`, `src/SourceMix/SourceMixCli.cs` | Add new command-line options here first, then thread them through the request object and runtime flow. |
| TUI entry and wizard flow | `src/SourceMix.Tui/SourceMixTuiApplication.cs`, `src/SourceMix.Tui/TuiWizard.cs` | `SourceMixTuiApplication` prepares solution context; `TuiWizard` owns step order, backtracking, and step-to-step state. |
| Shared TUI rendering | `src/SourceMix.Tui/TuiRender.cs`, `src/SourceMix.Tui/ListPickerPrompt.cs`, `src/SourceMix.Tui/TextInputPrompt.cs` | Header redraw, resize handling, and row accounting live here. Changes in these files usually affect multiple prompts. |
| Files step | `src/SourceMix.Tui/FileSearchPrompt.cs`, `src/SourceMix.Tui/ListPickerPrompt.cs` | The Files step is a composition of a stateful file prompt over the generic list picker. |
| Options and output steps | `src/SourceMix.Tui/OptionsPrompt.cs`, `src/SourceMix.Tui/OutputPathPrompt.cs`, `src/SourceMix.Tui/TextInputPrompt.cs` | Option toggles stay in `OptionsPrompt`; output-path validation is in `OutputPathPrompt`. |
| Prompt and skills steps | `src/SourceMix.Tui/PromptSelector.cs`, `src/SourceMix.Core/BuiltInPrompts.cs`, `src/SourceMix.Core/SkillScanner.cs` | Built-in prompts are code-backed; skills are file-backed. |
| Source shaping | `src/SourceMix.Core/SourceProcessor.cs`, `src/SourceMix.Core/TypeExpansionRewriter.cs`, `src/SourceMix.Core/SourceFile.cs` | `SourceProcessor` owns normalization and trim flow; semantic inferred-type expansion is isolated in `TypeExpansionRewriter`. |
| Dependency expansion | `src/SourceMix.Core/DependencyResolver.cs`, `src/SourceMix.Core/AssemblyDecompiler.cs` | Source-first resolution happens before optional decompilation fallback. |
| Preferences and persistence | `src/SourceMix.Core/PreferencesManager.cs`, `src/SourceMix.Core/SolutionPreferences.cs`, `src/SourceMix.Core/GlobalPreferences.cs` | Solution-scoped and global preferences are separate on purpose. |
| Markdown output | `src/SourceMix.Core/OutputFormatter.cs` | Output section order and headings are centralized here. |

## Common change recipes

### Add a CLI/TUI option

1. Add the CLI option in `SourceMixCommandFactory.cs`.
2. Thread it through `CliRequest.cs` and `SourceMixCli.cs`.
3. If the TUI also needs it, add it to `MixOptions.cs`, `OptionsToggleValues.cs`, and `OptionsPrompt.cs`.
4. Update the tests in both `test/SourceMix.Tests` and `test/SourceMix.Tui.Tests` if the option is available in both surfaces.

### Change wizard navigation or step persistence

Start in `TuiWizard.cs`. That file is the source of truth for:

- step order
- back navigation
- quit vs cancel behavior
- which values persist when the user returns to a previous step

If a change also affects what is visible during a step, expect follow-up work in
`FileSearchPrompt.cs`, `ListPickerPrompt.cs`, or `TextInputPrompt.cs`.

### Change visible TUI layout

Touch `TuiRender.cs` and the shared prompt classes before you change any
step-specific code. The most fragile area is row accounting:

- the shared app header must stay visible in the visible pane
- resize redraws must not leave stale rows behind
- validation and hint lines must be included in clear/redraw accounting

If you change list height or prompt height, expect to update both xUnit tests
and `scripts/test-tui.bash`.

### Change source processing

`SourceProcessor.cs` is the orchestration layer. Keep individual rewrite rules
in focused helpers where possible. For inferred-type expansion, use the semantic
batch path instead of adding more ad hoc syntax-only logic.

## Test layers

The TUI has two different regression layers:

| Layer | Purpose | Main locations |
| --- | --- | --- |
| Fast logic/state tests | Cursor movement, selection rules, step transitions, prompt validation, persistence | `test/SourceMix.Tui.Tests/*.cs` |
| Real-terminal visible-pane tests | Header persistence, resize redraw, overlap/backsliding, wrapped output cleanup | `scripts/test-tui.bash` |

Use the unit tests when you care about state transitions or return values. Use
the tmux suite when the question is "what does the user actually see?"

## TUI testing gotchas

- `TestTuiConsole` does **not** emulate terminal clearing. It records output and
  counts clear calls, which is good for logic checks but not for visible-pane
  truth.
- `scripts/test-tui.bash` should assert against the **visible pane**, not
  scrollback. Regressions around hidden headers or stale lines can look correct
  in scrollback while still being wrong on-screen.
- The tmux script is intentionally Bash-3-safe for macOS. Keep the `hj::`
  function prefix and the safe empty-array expansion style when editing it.

## Preferences and environment isolation

Tests that touch persisted prompts, skills, or solution preferences should avoid
the real user config directory. Follow the existing pattern of overriding
`HOME`, `XDG_CONFIG_HOME`, and `DOTNET_CLI_HOME` in real-terminal tests so the
developer machine state does not leak into test results.

## When to extend tmux coverage

Add a tmux scenario when a change affects:

- header visibility
- resize behavior
- line wrapping
- prompt/list overlap
- cross-step visible state after backtracking

If a bug report includes a screenshot-worthy terminal symptom, it probably
deserves tmux coverage.
