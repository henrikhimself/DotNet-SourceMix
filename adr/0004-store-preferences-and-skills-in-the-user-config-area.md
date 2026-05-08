# 0004 - Store preferences and skills in the user config area

## Status

Accepted

## Context

The TUI needs to remember recent user choices.

The user can also define global prompts and reusable skills.

This data must persist across runs and stay outside the repository.

## Decision

Store configuration in the user config area.

Store per-solution preferences in a hashed file name that is based on the normalized solution directory.

Store global prompt preferences in one global JSON file.

Store skills under a `skills` directory.

Use the directory name as the skill key.

Require one `SKILL.md` file in each skill directory.

## Consequences

Preferences do not add noise to the repository.

The same machine can keep different defaults for different solutions.

Skills are easy to add without a code change.
