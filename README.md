# Source Mix

A CLI for collecting multiple .NET C# source files into an LLM AI optimized file that can be uploaded to aid with coding tasks on existing code bases.

## Usage

```bash
sourcemix [<files>...] [--output <path>]
```

**Arguments:**

- `<files>` — one or more file paths or glob patterns (e.g. `src/**/*.cs`)

**Options:**

- `-o`, `--output <path>` — write output to a file instead of stdout

**Examples:**

```bash
# Collect all C# files in src/ and write to a Markdown file
sourcemix "src/**/*.cs" -o context.md

# Collect specific files and print to stdout
sourcemix Foo.cs Bar.cs Baz.cs
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

```bash
dotnet publish src/SourceMix -c Release -r <RID> --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

The executable is written to `src/SourceMix/bin/Release/net10.0/<RID>/publish/`.

**Example — macOS Apple Silicon:**

```bash
dotnet publish src/SourceMix -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

Copy the produced binary to a directory on your `PATH` to use it as a global command:

```bash
sudo cp src/SourceMix/bin/Release/net10.0/osx-arm64/publish/SourceMix /usr/local/bin/sourcemix
```
