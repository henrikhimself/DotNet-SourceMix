# 0006 - Use a Roslyn rewrite pipeline for source shaping

## Status

Accepted

## Context

Raw source files contain noise for LLM input.

The tool must reduce that noise but keep type and member information.

Some users also need explicit inferred types in the exported text.

## Decision

Use Roslyn syntax trees and rewriters to shape source before output.

Always remove using directives and comments.

Optionally trim dependency method bodies.

Optionally expand inferred local types and target-typed `new()` with semantic analysis.

Run semantic expansion only in batch mode where one compilation can see all selected files.

Keep unresolved and anonymous cases unchanged.

## Consequences

The exported code is smaller and easier to read.

The tool can keep one consistent shaping pipeline for CLI and TUI.

The semantic expansion feature adds compilation cost, but it improves clarity in the exported text.
