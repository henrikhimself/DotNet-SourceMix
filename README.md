# Source Mix

A CLI and interactive TUI for collecting multiple .NET C# source files into an LLM AI optimized file that can be uploaded to aid with coding tasks on existing code bases.

## Interactive TUI

Run the TUI from any directory containing a `.sln` or `.slnx` solution file:

```bash
sourcemix-tui
```

### Wizard flow

1. **Search & select files** — type to filter `.cs` files by name in real-time, navigate with `↑↓`, toggle selection with `Space`, pin a file with `P` (select + mark for next run), confirm with `Enter`. Press `Tab` to switch to the Pinned view. Pinned files from previous runs are preselected and marked with `●`.
2. **Pinned files view** — press `Tab` to switch to a list of all pinned files. Use `↑↓` to navigate and `Space` or `P` to unpin (and deselect) individual files. Press `Tab` again to return to search.
2. **Configure options** — choose recursive dependency resolution, recursion depth limit, and whether to decompile types from compiled assemblies. Defaults are loaded from saved preferences.
3. **Generate** — output is written to `~/sourcemix.md` by default (configurable). Preferences are auto-saved for the next run.

> **Note:** The `--include-compiled` option requires a prior `dotnet build` so assemblies exist in the `bin/` directories.

### Preferences

SourceMix saves per-solution preferences automatically after each TUI run. Preferences include pinned files, output path, and default option values.

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
  "outputPath": "/custom/output.md",
  "defaults": {
    "recursive": true,
    "limitDepth": false,
    "maxDepth": 3,
    "includeCompiled": false
  }
}
```

- **pinnedFiles** — relative paths from the solution root; explicitly pinned with `P` in the file picker, preselected on the next run
- **outputPath** — overrides the default `~/sourcemix.md`; omit to use the default
- **defaults** — default answers for each wizard prompt

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

**Examples:**

```bash
# Collect all C# files in src/ and write to a Markdown file
sourcemix "src/**/*.cs" -o context.md

# Collect specific files and print to stdout
sourcemix Foo.cs Bar.cs Baz.cs

# Recursively include referenced source types and decompile unresolved NuGet types
sourcemix MyService.cs -r -c -o context.md
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
dotnet publish src/SourceMix -c Release -r <RID> --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

**TUI:**

```bash
dotnet publish src/SourceMix.Tui -c Release -r <RID> --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

The executables are written to `src/<Project>/bin/Release/net10.0/<RID>/publish/`.

**Example — macOS Apple Silicon:**

```bash
dotnet publish src/SourceMix -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
dotnet publish src/SourceMix.Tui -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

Copy the produced binaries to a directory on your `PATH` to use them as global commands:

```bash
sudo cp src/SourceMix/bin/Release/net10.0/osx-arm64/publish/SourceMix /usr/local/bin/sourcemix
sudo cp src/SourceMix.Tui/bin/Release/net10.0/osx-arm64/publish/SourceMix.Tui /usr/local/bin/sourcemix-tui
```
