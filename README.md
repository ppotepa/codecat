# codecat

`codecat` is a small Native AOT C# CLI that recursively scans a codebase and concatenates the files that matter into one LLM-friendly text file.

It is meant for mixed-language projects where a single repository can contain C#, Android/Kotlin, C++, Python, TypeScript, SQL, shell scripts, config files, docs, and other user-authored source files. Build outputs, dependency folders, caches, and generated artifacts are ignored by default.

The goal is not to make a pretty human report. The goal is to produce a stable text container that an LLM can read quickly:

- clear global metadata
- one explicit section per file
- path, plugin, language, line count, byte count, and SHA-256 per file
- deterministic ordering
- no `bin/`, `obj/`, `node_modules/`, `target/`, `.venv/`, `.gradle/`, build caches, or previous `codecat` outputs

## Usage

Run from source:

```powershell
dotnet run -- . -o concat.txt
```

Scan another project:

```powershell
dotnet run -- D:\Git\my-app -o D:\Git\my-app\concat.txt
```

Publish a native Windows executable:

```powershell
dotnet publish -c Release -r win-x64
.\bin\Release\net10.0\win-x64\publish\Codecat.exe . -o concat.txt
```

The project is configured for Native AOT, so the published `Codecat.exe` does not require a separate .NET runtime on the target machine.

## Output Shape

```text
CODECAT_VERSION: 1
ROOT: D:\Git\my-app
TOTAL_FILES: 3
TOTAL_LINES: 120
TOTAL_BYTES: 4096
PLUGIN_COUNTS: csharp=2;python=1

<<<FILE path="Program.cs" plugin="csharp" lang="csharp" lines="80" bytes="2048" sha256="...">>>
...
<<<END_FILE>>>

<<<SUMMARY>>>
included_files=3
total_lines=120
total_bytes=4096
plugin_counts=csharp=2;python=1
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
