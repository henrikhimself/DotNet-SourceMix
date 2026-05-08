# 0005 - Use a stateful TUI wizard with shared redraw infrastructure

## Status

Accepted

## Context

The TUI has multiple steps.

The user must move forward and backward without data loss.

The terminal can also need redraws after resize or view changes.

## Decision

Use an explicit wizard state machine.

Keep selected files, options, output path, prompt, and skills in wizard state.

Use shared picker and text-input controls for interactive steps.

Use one shared header render path and one shared full-screen redraw helper.

Reserve vertical space for header and control chrome in list rendering.

## Consequences

The TUI keeps user work when the user goes back.

Common redraw behavior stays in one place.

The header stays visible across full-screen redraw paths.
