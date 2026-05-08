# 0003 - Resolve dependencies from source first and use decompilation as fallback

## Status

Accepted

## Context

The selected seed files are often not enough.

The user can need related types from the same solution.

Some referenced types are not present in source files.

## Decision

Resolve dependencies from source files first.

Build a type-to-file index from the solution tree.

Use breadth-first traversal to add referenced types.

Ignore `bin` and `obj` directories during source discovery.

If a type is not found in source, record it as unresolved.

If compiled fallback is on, scan assemblies under `bin` and decompile only:

- interfaces
- simple model classes
- simple model structs

## Consequences

The tool prefers project source over compiled output.

The output stays closer to the solution code that the user can edit.

Compiled fallback adds useful context without full assembly dumps.
