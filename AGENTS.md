# Agent instructions

## Project overview
This solutions contains console application which implements a CLI for collecting .NET C# files into an LLM AI optimized file that can be uploaded to aid with coding tasks on existing code bases.

The `src\SourceMix` directory implements the core functionality of the CLI tool.

### Technology Stack
- .NET 10.0
- C#
- Multi-platform support (Windows, Linux, macOS, containers)

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
