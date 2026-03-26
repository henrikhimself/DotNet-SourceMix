# Agent instructions

## Project overview

This solution contains two console applications:

- **`src/SourceMix`** — CLI tool for collecting .NET C# files into an LLM AI optimized Markdown file.
- **`src/SourceMix.Tui`** — Interactive TUI wizard that wraps the same functionality with a keyboard-driven file picker, option prompts, prompt personality selector, and skill selector.

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
| `src/SourceMix.Tui/FileSearchPrompt.cs` | Raw-console file picker (Search / Pinned / Selected views) |
| `src/SourceMix.Tui/OptionsPrompt.cs` | Spectre.Console wizard step for processing options |
| `src/SourceMix.Tui/PromptSelector.cs` | Spectre.Console `SelectionPrompt` for choosing a prompt personality |
| `src/SourceMix.Tui/SkillSelectionPrompt.cs` | Raw-console single-view skill selector with pinning |
| `src/SourceMix.Tui/CsFileScanner.cs` | Scans solution for `.cs` files (excludes `bin/` and `obj/`) |
| `src/SourceMix.Tui/Program.cs` | TUI orchestration: load prefs → scan → select files → options → prompt → skills → process → write → save |
| `src/SourceMix/Program.cs` | CLI entry point using `System.CommandLine` |

## General

* Make only high confidence suggestions when reviewing code changes.
* Always use the version of C# that matches the latest .NET LTS.
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

