# Agent instructions

## Project overview

This solution ships as a single dotnet tool command:

- **`src/SourceMix`** — Tool host and CLI entry point. Standard argument-driven usage runs the non-interactive CLI flow, and the `tui` subcommand launches the interactive workflow.
- **`src/SourceMix.Tui`** — Interactive TUI library used by the tool host for the keyboard-driven file picker, option prompts, prompt personality selector, and skill selector.

Both tools share a common library:

- **`src/SourceMix.Core`** — Core logic: dependency resolution, source processing, output formatting, preferences management, skill scanning, decompilation.

### Technology Stack

- .NET 10.0
- C#
- Multi-platform support (Windows, Linux, macOS, containers)

## Documentation sources

- **`SPEC.md`** — Product specification for the CLI and TUI. Read this file before you change user-visible behavior, command options, workflow steps, output format, or configuration rules.
- **`adr/`** — Architecture decision records. Read the ADR files before you change cross-cutting design, shared infrastructure, storage locations, output structure, dependency resolution, or source-processing behavior.

### ADR storage

- Store new architecture decision records in the repository root `adr/` directory.
- Use one Markdown file per decision.
- Use a numeric prefix in the file name, for example `0007-some-decision.md`.
- Update `adr/README.md` when you add a new ADR.

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

```bash
# Run the tmux-driven real-terminal TUI smoke tests directly
./scripts/test-tui.bash
```

### Real-terminal TUI validation

- `tmux` is available and must be used when changes affect TUI redraw, resize handling, header persistence, wrapped output cleanup, or keyboard-driven screen flow.
- `./scripts/test.bash` now includes the tmux TUI smoke suite by default. Run `./scripts/test-tui.bash` directly when you need a faster loop on terminal-specific behavior.
- Keep the xUnit tests in `test/SourceMix.Tui.Tests` for fast logic coverage, but do not treat them as sufficient proof for terminal rendering behavior.
- For autonomous agent-driven TUI work, use tmux to:
  - start `SourceMix tui` inside a real terminal session
  - send keys with `tmux send-keys`
  - resize the terminal with `tmux resize-window`
  - inspect the rendered screen with `tmux capture-pane`
- When a bug is about what the user can see in the terminal, prefer the tmux path over adding more fake-console assertions.

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
