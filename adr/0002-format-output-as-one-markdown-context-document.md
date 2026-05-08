# 0002 - Format output as one Markdown context document

## Status

Accepted

## Context

The tool prepares code for LLM input.

The output must be easy to read and easy to paste or save.

The output must also combine source, optional dependency text, and optional instruction text.

## Decision

Format the result as one Markdown document.

Write source in fenced `csharp` code blocks.

Write decompiled dependencies in a separate section.

Write prompt text in a final instructions section.

Write skills before the code section.

## Consequences

The output is stable and simple.

The user can send the file to an LLM with little extra work.

The tool can stream to standard output or write to a file without a format change.
