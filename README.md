# codecat

`codecat` is a small Native AOT C# CLI that recursively scans a codebase and concatenates the files that matter into one LLM-friendly text file.

It is meant for mixed-language projects where a single repository can contain C#, Android/Kotlin, C++, Python, TypeScript, SQL, shell scripts, config files, docs, and other user-authored source files. Build outputs, dependency folders, caches, and generated artifacts are ignored by default.

The goal is not to make a pretty human report. The goal is to produce a stable text container that an LLM can read quickly:

- clear global metadata
- one explicit section per file
- path, plugin, language, line count, byte count, and SHA-256 per file
- deterministic ordering
- no `bin/`, `obj/`, `node_modules/`, `target/`, `.venv/`, `.gradle/`, build caches, or previous `codecat` outputs

Defaults are intentionally restrictive. Hidden directories are skipped unless explicitly allowed by the scanner, common binary/asset extensions are ignored before plugin matching, and files larger than 250 KB are skipped unless `--max-file-bytes` is increased.

## Usage

Run from source:

```powershell
dotnet run --project src/Codecat -- . -o concat.txt
```

Scan another project:

```powershell
dotnet run --project src/Codecat -- D:\Git\my-app -o D:\Git\my-app\concat.txt
```

Publish a native Windows executable:

```powershell
dotnet publish src/Codecat -c Release -r win-x64
.\src\Codecat\bin\Release\net10.0\win-x64\publish\Codecat.exe . -o concat.txt
```

The project is configured for Native AOT, so the published `Codecat.exe` does not require a separate .NET runtime on the target machine.

Build release artifacts:

```powershell
.\scripts\build-release.ps1
```

The script restores the repo-local WiX tool, publishes a Native AOT build, creates a ZIP archive, and builds a Windows MSI installer.

This creates:

```text
artifacts/release/codecat-0.2.0-win-x64/
artifacts/release/codecat-0.2.0-win-x64.zip
artifacts/release/codecat-0.2.0-win-x64.msi
```

The MSI installs `Codecat.exe` into `Program Files\Codecat` and adds that folder to the system `PATH`.

After installing the MSI, open a new terminal and run:

```powershell
codecat --help
```

Because Windows resolves executables case-insensitively, the installed `Codecat.exe` can be launched as `codecat` from any directory.

Common options:

```powershell
codecat .                         # scan the current directory
codecat . -o context.txt           # choose output file
codecat . --quiet                  # suppress progress output
codecat . --verbose                # print detailed skip information
codecat . --max-file-bytes 250000  # skip larger files
codecat --list-plugins             # show built-in plugin rules
codecat --version
```

## Output Shape

```text
CODECAT_VERSION: 1
ROOT: D:\Git\my-app
TOTAL_FILES: 3
TOTAL_LINES: 120
TOTAL_BYTES: 4096
PLUGIN_COUNTS: csharp=2;python=1
DIRECTORIES_VISITED: 12
FILES_SEEN: 40
ITEMS_SKIPPED: 37
WARNINGS: 0
SKIPPED_BY_REASON: ignored_directory=10;no_plugin_match=27

<<<FILE path="Program.cs" plugin="csharp" lang="csharp" lines="80" bytes="2048" sha256="...">>>
...
<<<END_FILE>>>

<<<SUMMARY>>>
included_files=3
total_lines=120
total_bytes=4096
plugin_counts=csharp=2;python=1
directories_visited=12
files_seen=40
items_skipped=37
warnings=0
skipped_by_reason=ignored_directory=10;no_plugin_match=27
<<<END_SUMMARY>>>
```

Plugins are compiled into the binary. This keeps the first version compatible with Native AOT and avoids dynamic loading/reflection constraints.

## Built-in Plugins

The first version ships with 30 compiled-in plugins:

```text
csharp
java
kotlin
android
swift
objective-c
cpp
c
rust
go
python
javascript
typescript
web
php
ruby
dart-flutter
lua
r
julia
scala
elixir
erlang
haskell
fsharp
powershell
shell
sql
config
docs
```

## Current Scope

This is the first working version. It focuses on:

- recursive project scanning
- plugin-based file inclusion
- artifact/cache/dependency exclusion
- one output file
- LLM-friendly metadata and delimiters
- Native AOT publishing

Planned directions include configurable plugin rules, size limits, explicit include/exclude patterns, and output variants.

## Repository Layout

```text
installer/wix/  Windows MSI definition
scripts/        release/build scripts
src/Codecat/
  Cli/        command-line parsing
  Output/     concat.txt writer
  Plugins/    built-in language/ecosystem rules
  Scanning/   recursive project scanner
```

## License

MIT
