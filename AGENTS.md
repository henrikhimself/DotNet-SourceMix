# Agent instructions

## Project overview

This solution now ships as a single dotnet tool command:

- **`src/SourceMix`** — Tool host and CLI entry point. Standard argument-driven usage runs the non-interactive CLI flow, and the `tui` subcommand launches the interactive workflow.
- **`src/SourceMix.Tui`** — Interactive TUI library used by the tool host for the keyboard-driven file picker, option prompts, prompt personality selector, and skill selector.

Both tools share a common library:

- **`src/SourceMix.Core`** — Core logic: dependency resolution, source processing, output formatting, preferences management, skill scanning, decompilation.

### Technology Stack

- .NET 10.0
- C#
- Multi-platform support (Windows, Linux, macOS, containers)

### Key source files

| File | Purpose |
|---|---|
| `src/SourceMix.Core/DependencyResolver.cs` | Recursive type-dependency resolution using Roslyn; namespace-aware disambiguation |
| `src/SourceMix.Core/SourceProcessor.cs` | Strips usings/comments from C# source; optional `TrimRewriter` removes method bodies |
| `src/SourceMix.Core/OutputFormatter.cs` | Writes skills, code blocks, decompiled section, and prompt to a `TextWriter` |
| `src/SourceMix.Core/BuiltInPrompts.cs` | 7 named built-in prompt personalities |
| `src/SourceMix.Core/GlobalPreferences.cs` | Stores custom prompts shared across all solutions |
| `src/SourceMix.Core/SolutionPreferences.cs` | Per-solution preferences: pinned files, pinned skills, default prompt key, option defaults |
| `src/SourceMix.Core/PreferencesManager.cs` | Load/save solution and global prefs; `GetSkillsDirectory()` |
| `src/SourceMix.Core/SkillScanner.cs` | Scans `skills/<Name>/SKILL.md` in the config directory |
| `src/SourceMix.Core/AssemblyDecompiler.cs` | Decompiles interfaces and simple models from `bin/` DLLs using ICSharpCode.Decompiler |
| `src/SourceMix/SourceMixCommandFactory.cs` | Builds the single `sourcemix` command surface and routes `tui` to the interactive flow |
| `src/SourceMix/SourceMixCli.cs` | Non-interactive CLI execution logic used by the dotnet tool host |
| `src/SourceMix.Tui/FileSearchPrompt.cs` | Raw-console file picker (Search / Pinned / Selected views) |
| `src/SourceMix.Tui/OptionsPrompt.cs` | Spectre.Console wizard step for processing options |
| `src/SourceMix.Tui/PromptSelector.cs` | Spectre.Console `SelectionPrompt` for choosing a prompt personality |
| `src/SourceMix.Tui/SkillSelectionPrompt.cs` | Raw-console single-view skill selector with pinning |
| `src/SourceMix.Tui/CsFileScanner.cs` | Scans solution for `.cs` files (excludes `bin/` and `obj/`) |
| `src/SourceMix.Tui/SourceMixTuiApplication.cs` | TUI orchestration: load prefs → scan → select files → options → prompt → skills → process → write → save |
| `src/SourceMix/Program.cs` | Minimal tool entry point that invokes the command factory |
| `src/SourceMix/SourceMix.csproj` | Dotnet-tool packaging metadata, packaged README, and packaged LICENSE configuration |
| `Directory.Build.targets` | Shared project-wide metadata such as authors and copyright |
| `LICENSE` | MIT license text that must stay consistent with the package metadata |

## Common Commands

### Dotnet CLI tools

```bash
# Install the dotnet CLI tools if they are missing
dotnet tool restore
```

### Development

```bash
# Restore the package dependencies
./scripts/restore.bash
```

```bash
# Build the solution
./scripts/build.bash
```

### Testing

```bash
# Build and run unit tests
./scripts/test.bash
```

### Benchmarks

```bash
# Run all benchmarks and save results as Markdown
./scripts/benchmark.bash
```

Benchmark output is suppressed during the run. Results are printed as Markdown when complete.

Each run saves its output to `tmp/benchmark-latest.md`. The previous run is automatically preserved as `tmp/benchmark-previous.md`. Both files are gitignored and local to the developer's machine.

#### What each benchmark class measures

| Class | File | What it measures |
|---|---|---|
| `DependencyResolverBenchmarks` | `test/SourceMix.Benchmarks/DependencyResolverBenchmarks.cs` | Cost of resolving a type graph across two topologies (chain, island) at three scales (100, 500, 1000 nodes). Covers the lazy-loading and BFS phases in `DependencyResolver`. |
| `SourceProcessorBenchmarks` | `test/SourceMix.Benchmarks/SourceProcessorBenchmarks.cs` | Cost of Roslyn rewriting per file. Three realistic source sizes (small ≈80 lines, medium ≈250 lines, large ≈500 lines) × strip-only vs trim-bodies. Covers `SourceProcessor` and `TrimRewriter`. |
| `FileSearchPromptBenchmarks` | `test/SourceMix.Benchmarks/FileSearchPromptBenchmarks.cs` | Perceived search lag in the TUI file picker. Four scenarios: sequential typing (baseline), incremental typing (caching), navigation without cache, navigation with cache. Covers `FileSearchPrompt` filtering and cache-invalidation logic. |

#### Using benchmarks to evaluate code changes

Before making a performance-sensitive change, run the benchmarks once to establish a baseline. After the change, run again. Compare the two files to determine whether the change improved or degraded performance:

```bash
diff tmp/benchmark-previous.md tmp/benchmark-latest.md
```

Or read both files directly to compare `Mean` and `Allocated` values for each benchmark method.

**Decision guidance for agents:**

- A reduction in `Mean` time is a performance improvement.
- A reduction in `Allocated` bytes is a memory improvement.
- An increase in either metric is a regression and should be investigated before proceeding.
- If neither file exists yet, run `./scripts/benchmark.bash` to create the baseline before making changes.
- When a specific benchmark class regresses, use the table above to find the relevant source files.

### Packing

```bash
# Create the release package for local install or publishing
./scripts/pack.bash
```

## General

* Make only high confidence suggestions when reviewing code changes.
* Always use the version of C# that matches the latest .NET LTS.
* Never disable analyzers. Instead fix the errors that are shown when building.
* Never change global.json unless explicitly asked to.
* Never change package.json or package-lock.json files unless explicitly asked to.
* Never change NuGet.config files unless explicitly asked to.

## Formatting

* Apply code-formatting style defined in `.editorconfig`.
* Prefer file-scoped namespace declarations and single-line using directives.
* Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
* Ensure that the final return statement of a method is on its own line.
* Use pattern matching and switch expressions wherever possible.
* Use `nameof` instead of string literals when referring to member names.
* Place private class declarations at the bottom of the file.

### Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

## Markdown files

* Markdown files should not have multiple consecutive blank lines.
* Code blocks should be formatted with triple backticks (```) and include the language identifier for syntax highlighting.
* JSON code blocks should be indented properly.

## Trust These Instructions

These instructions are comprehensive and tested. Only search for additional information if:

1. The instructions appear outdated or incorrect
2. You encounter specific errors not covered here
3. You need details about new features not yet documented
