# Source Mix

A CLI and interactive TUI for collecting multiple .NET C# source files into an LLM AI optimized file that can be uploaded to aid with coding tasks on existing code bases.

## Interactive TUI

Run the TUI from any directory containing a `.sln` or `.slnx` solution file:

```bash
sourcemix-tui
```

### Wizard flow

1. **Search & select files** — type to filter `.cs` files by name in real-time, navigate with `↑↓`, toggle selection with `Space`, pin a file with `Ctrl+P` (select + mark for next run), confirm with `Enter`. Press `Tab` to switch to the Pinned view. Pinned files from previous runs are preselected and marked with `*`.
2. **Pinned files view** — press `Tab` to switch to a list of all pinned files. Use `↑↓` to navigate and `Space` or `Ctrl+P` to unpin (and deselect) individual files. Press `Tab` again to go to the Selected view.
3. **Selected files view** — press `Tab` to see all currently selected files. Use `Space` to deselect. Press `Tab` again to return to search.
4. **Configure options** — choose recursive dependency resolution, recursion depth limit, whether to decompile types from compiled assemblies, and whether to trim method bodies from dependency files (keep signatures only). Defaults are loaded from saved preferences.
5. **Select prompt** — optionally append a built-in prompt personality (`unit-test`, `code-review`, `tech-docs`, `explain`, `debug`, `refactor`, `architecture`) or a saved custom prompt to the output. The last-used prompt is preselected. Custom prompts are shared across all solutions.
6. **Select skills** — optionally prepend one or more skill instruction files to the output. Skills are Markdown files stored in the skills config directory. Multiple skills can be selected; pin with `Ctrl+P` to preselect on the next run. This step is skipped automatically when no skills exist.
7. **Generate** — output is written to `~/sourcemix.md` by default (configurable). Preferences are auto-saved for the next run.

> **Note:** The `--include-compiled` option requires a prior `dotnet build` so assemblies exist in the `bin/` directories.

### Preferences

SourceMix saves per-solution preferences automatically after each TUI run. Preferences include pinned files, pinned skills, output path, last-used prompt, and default option values.

**Config file location:**

| Platform | Location |
|---|---|
| Windows | `%APPDATA%\sourcemix\sourcemix-<hash>.json` |
| Linux | `~/.config/sourcemix/sourcemix-<hash>.json` |
| macOS | `~/.config/sourcemix/sourcemix-<hash>.json` |

Each solution gets its own preferences file, identified by a hash of the solution directory path.

**Schema:**

```json
{
  "solutionPath": "/path/to/solution",
  "pinnedFiles": [
    "src/MyProject/Service.cs",
    "src/MyProject/Model.cs"
  ],
  "pinnedSkills": [
    "unit-test-style"
  ],
  "defaultPromptKey": "code-review",
  "outputPath": "/custom/output.md",
  "defaults": {
    "recursive": true,
    "limitDepth": false,
    "maxDepth": 3,
    "includeCompiled": false,
    "trim": false
  }
}
```

- **pinnedFiles** — relative paths from the solution root; explicitly pinned with `Ctrl+P` in the file picker, preselected on the next run
- **pinnedSkills** — skill keys (directory names) pinned with `Ctrl+P` in the skill selector, preselected on the next run
- **defaultPromptKey** — key of the last-used prompt; that prompt is highlighted first in the selector on the next run
- **outputPath** — overrides the default `~/sourcemix.md`; omit to use the default
- **defaults** — default answers for each wizard prompt; `trim` strips method bodies from dependency files

### Global preferences

Custom prompts are stored in a single global file shared across all solutions:

| Platform | Location |
|---|---|
| Windows | `%APPDATA%\sourcemix\sourcemix-global.json` |
| Linux | `~/.config/sourcemix/sourcemix-global.json` |
| macOS | `~/.config/sourcemix/sourcemix-global.json` |

**Schema:**

```json
{
  "customPrompts": {
    "our-review": "Review against our team coding standards...",
    "sprint-demo": "Summarise changes suitable for a sprint review demo..."
  }
}
```

### Skills

Skills are reusable Markdown instruction files that are prepended to the output before the code blocks and prompt. Each skill lives in its own subdirectory of the skills config directory and must contain a `SKILL.md` file. The directory name is the skill key.

**Skills directory:**

| Platform | Location |
|---|---|
| Windows | `%APPDATA%\sourcemix\skills\` |
| Linux | `~/.config/sourcemix/skills\` |
| macOS | `~/.config/sourcemix/skills\` |

**Example structure:**

```
~/.config/sourcemix/skills/
  unit-test-style/
    SKILL.md      ← skill instructions for this project's unit test conventions
  architecture-rules/
    SKILL.md      ← architecture and naming rules
```

**Output order** when skills, code, and a prompt are all present:

1. Skill content (raw `SKILL.md` text, one blank line after each)
2. Code blocks (selected source files and any decompiled dependencies)
3. Prompt instructions (`## Instructions` section)

## CLI Usage

```bash
sourcemix [<files>...] [--output <path>] [--recursive] [--depth <n>] [--include-compiled]
```

**Arguments:**

- `<files>` — one or more file paths or glob patterns (e.g. `src/**/*.cs`)

**Options:**

- `-o`, `--output <path>` — write output to a file instead of stdout
- `-r`, `--recursive` — recursively include files that define types referenced by the specified files
- `-d`, `--depth <n>` — maximum recursion depth when `--recursive` is used (defaults to no limit)
- `-c`, `--include-compiled` — decompile interfaces and simple model types from compiled assemblies in `bin/` for types not found in source; requires `--recursive` and a prior `dotnet build`
- `-t`, `--trim` — strip method bodies from dependency files, keeping type signatures only; requires `--recursive`
- `-p`, `--prompt <name-or-text>` — append a prompt personality to the output; use a built-in key (`unit-test`, `code-review`, `tech-docs`, `explain`, `debug`, `refactor`, `architecture`) or provide custom text
- `-s`, `--skills <key>...` — prepend one or more skill instruction files to the output; each value is a skill key (subdirectory name under the skills config directory)

**Examples:**

```bash
# Collect all C# files in src/ and write to a Markdown file
sourcemix "src/**/*.cs" -o context.md

# Collect specific files and print to stdout
sourcemix Foo.cs Bar.cs Baz.cs

# Recursively include referenced source types and decompile unresolved NuGet types
sourcemix MyService.cs -r -c -o context.md

# Trim dependency method bodies and append a unit-test prompt
sourcemix MyService.cs -r -t --prompt unit-test -o context.md

# Prepend skills, trim dependencies, and append a code-review prompt
sourcemix MyService.cs -r -t --skills unit-test-style architecture-rules --prompt code-review -o context.md
```

## Publish as a self-contained executable

A self-contained executable bundles the .NET runtime so the target machine does not need .NET installed.

Replace `<RID>` with the target platform:

| Platform | RID |
|---|---|
| Windows x64 | `win-x64` |
| macOS x64 (Intel) | `osx-x64` |
| macOS ARM64 (Apple Silicon) | `osx-arm64` |
| Linux x64 | `linux-x64` |
| Linux ARM64 | `linux-arm64` |

**CLI:**

```bash
dotnet publish src/SourceMix -c Release -r <RID> --self-contained true -p:PublishSingleFile=true
```

**TUI:**

```bash
dotnet publish src/SourceMix.Tui -c Release -r <RID> --self-contained true -p:PublishSingleFile=true
```

The executables are written to `src/<Project>/bin/Release/net10.0/<RID>/publish/`.

> **Note:** Avoid `-p:PublishTrimmed=true`. The Roslyn and ICSharpCode.Decompiler libraries used for dependency resolution and decompilation are not trim-safe and will fail at runtime when trimmed.

**Example — macOS Apple Silicon:**

```bash
dotnet publish src/SourceMix -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
dotnet publish src/SourceMix.Tui -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

Copy the produced binaries to a directory on your `PATH` to use them as global commands:

```bash
sudo cp src/SourceMix/bin/Release/net10.0/osx-arm64/publish/SourceMix /usr/local/bin/sourcemix
sudo cp src/SourceMix.Tui/bin/Release/net10.0/osx-arm64/publish/SourceMix.Tui /usr/local/bin/sourcemix-tui
```

