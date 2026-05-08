# 0001 - Use one .NET tool host for CLI and TUI

## Status

Accepted

## Context

SourceMix supports two ways to work:

- a command-line interface for repeatable runs
- a terminal user interface for guided runs

The codebase must keep one product identity and one installation path.

## Decision

Use one .NET tool host as the entry point.

The host selects the mode from command input.

Standard arguments run the CLI flow.

The `tui` subcommand runs the interactive wizard.

## Consequences

The user installs and updates one tool.

The CLI and TUI can share the same core library.

The project can keep one package and one command name.
