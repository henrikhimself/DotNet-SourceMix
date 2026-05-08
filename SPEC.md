# SourceMix Specification

## 1. Purpose

SourceMix makes one Markdown context file from selected C# source files.

Use this tool when you need input for an LLM.

The tool has two user interfaces:

- CLI
- TUI

## 2. Product summary

SourceMix reads C# source files.

It can also add related source files.

It can also add decompiled dependency types from compiled assemblies.

It can prepend skill text.

It can append one prompt block.

The output is Markdown.

## 3. CLI

### 3.1 Start command

Use this command:

```bash
sourcemix [<files>...] [--output <path>] [--recursive] [--depth <n>] [--include-compiled] [--trim] [--expand-types] [--prompt <name-or-text>] [--skills <key>...]
```

### 3.2 Input files

You can give one or more file paths.

You can also give glob patterns such as `src/**/*.cs`.

If you do not give `--output`, the tool writes to standard output.

If you give `--output`, the tool writes to that file path.

### 3.3 CLI options

| Option | Function | Rule |
| --- | --- | --- |
| `-o`, `--output <path>` | Write output to a file. | The output directory must exist. |
| `-r`, `--recursive` | Add source files that define referenced types. | Use this option when you need dependency files. |
| `-d`, `--depth <n>` | Limit recursive depth. | This option works with `--recursive`. |
| `-c`, `--include-compiled` | Add decompiled dependency types from compiled assemblies. | This option requires `--recursive`. Run `dotnet build` first. |
| `-t`, `--trim` | Keep signatures but remove method bodies from dependency files. | This option requires `--recursive`. |
| `-e`, `--expand-types` | Replace `var` with inferred types and expand target-typed `new()`. | The tool does not change anonymous types, tuple deconstruction, or unresolved cases. |
| `-p`, `--prompt <name-or-text>` | Append one prompt block. | You can use a built-in key or custom text. |
| `-s`, `--skills <key>...` | Prepend one or more skills. | Each key must match one skill directory name. |

### 3.4 CLI processing rules

The CLI resolves the seed files first.

If `--recursive` is on, the CLI resolves related source files from the solution directory.

If `--include-compiled` is on, the CLI scans `bin` directories for assemblies.

The CLI only decompiles interfaces and simple model types.

The CLI warns if a requested skill is not present.

The CLI stops with an error if:

- `--trim` is used without `--recursive`
- `--include-compiled` is used without `--recursive`
- the output directory does not exist
- the output path cannot be written

## 4. TUI

### 4.1 Start command

Use this command:

```bash
sourcemix tui
```

Run it in a directory that contains a `.sln` or `.slnx` file, or in a child directory of that directory.

### 4.2 TUI header

The TUI shows one shared app header.

The header identifies the product.

The shared wizard header shows only the product name.

### 4.3 Wizard steps

The TUI uses a wizard with these steps:

1. Search and select files
2. Configure options
3. Set the output path
4. Select one prompt
5. Select skills
6. Generate output

The TUI keeps user choices when the user moves back to an earlier step.

### 4.4 File step

The file step shows three views:

- Search
- Pinned
- Selected

Use these keys:

- `Up` and `Down` to move the cursor
- `Space` to select or clear a file
- `Ctrl+P` to pin or unpin a file
- `Tab` to change the view
- `Ctrl+R` to clear the selected set in Search view
- `Ctrl+U` to clear the search text
- `Enter` to confirm
- `Ctrl+Q` to go back
- `Esc` to quit

### 4.5 Options step

The options step can set:

- recursive dependency resolution
- depth limit
- compiled dependency decompilation
- dependency body trim
- type expansion

If depth limit is on, the TUI asks for a positive integer.

### 4.6 Output step

The user sets one output file path.

If that file already exists, the TUI asks the user to:

- overwrite
- append
- cancel

### 4.7 Prompt and skills steps

The prompt step can use one built-in prompt or one custom prompt.

The skills step can select zero or more skills.

Each skill must come from a directory that contains one `SKILL.md` file.

## 5. Output format

### 5.1 Overall order

The output order is:

1. skill content, if present
2. source code section
3. decompiled dependencies section, if present
4. instructions section, if a prompt is present

### 5.2 Markdown structure

Source files are written in fenced C# code blocks.

The main source section starts with:

```markdown
---
# Source code
```

If decompiled dependencies exist, the output adds:

```markdown
## Decompiled Dependencies
```

If a prompt exists, the output adds:

```markdown
---
# Instructions
```

## 6. Source processing rules

The source processor removes:

- using directives
- comments
- XML documentation comments

It also removes extra blank lines.

It moves opening braces to the previous line in the final output.

If trim is on for dependency files, the processor removes method bodies but keeps signatures.

If expand-types is on, the processor uses Roslyn semantic analysis.

It can:

- replace local `var` with the inferred type
- replace `foreach (var ...)` with the inferred element type
- replace target-typed `new()` with explicit type text

It does not expand:

- anonymous types
- tuple deconstruction
- unresolved or error types

## 7. Dependency resolution

Source dependency resolution uses the solution directory as the search root.

The resolver:

1. collects `.cs` files
2. ignores `bin` and `obj`
3. builds a type-to-file index
4. walks referenced type names with breadth-first traversal
5. uses namespace data to narrow ambiguous matches

If a type is not found in source, the resolver records it as unresolved.

The decompiler can then try to supply that type from compiled assemblies.

## 8. Preferences and skills

SourceMix stores per-solution preferences.

These preferences include:

- pinned files
- pinned skills
- output path
- default prompt key
- default option values

SourceMix stores global custom prompts in a separate file.

The tool stores skills in the user config directory under `skills`.

Each skill uses one directory name as its key.

Each skill directory must contain `SKILL.md`.

## 9. Built-in prompts

The built-in prompt keys are:

- `nunit-test`
- `xunit-test`
- `code-review`
- `tech-docs`
- `explain`
- `debug`
- `refactor`
- `architecture`

## 10. Constraints

SourceMix is for C# source input.

The TUI requires a solution file.

Compiled dependency support depends on assemblies in `bin`.

The tool does not stop when one assembly cannot be decompiled.

It skips that assembly and continues.
