# codecat

`codecat` is a small Native AOT C# CLI that recursively scans a codebase and concatenates the files that matter into one LLM-friendly text file.

It is meant for mixed-language projects where a single repository can contain C#, Android/Kotlin, C++, Python, TypeScript, SQL, shell scripts, config files, docs, and other user-authored source files. Build outputs, dependency folders, caches, and generated artifacts are ignored by default.

The goal is not to make a pretty human report. The goal is to produce a stable text container that an LLM can read quickly:

- clear global metadata
- one explicit section per file
- path, plugin, language, line count, byte count, and SHA-256 per file
- deterministic ordering
- no `bin/`, `obj/`, `node_modules/`, `target/`, `.venv/`, `.gradle/`, build caches, or previous `codecat` outputs

Defaults are intentionally restrictive. The scanner uses `global deny -> plugin allowlist match -> safety filters -> include`. Hidden directories are skipped unless explicitly allowed by the scanner, Markdown `.md` files are ignored by default, common binary/asset extensions are ignored before plugin matching, and files larger than 250 KB are skipped unless `--max-file-bytes` is increased.

Use `--all` to include broader optional source/docs files such as Markdown `.md` files while keeping global safety and dependency exclusions enabled.

Use `--use-gitignore` when you want `codecat` to also exclude paths matched by `.gitignore` files in the scanned tree. When enabled, `.gitignore` rules run after global denies and before plugin matching.

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
artifacts/release/codecat-0.31-win-x64/
artifacts/release/codecat-0.31-win-x64.zip
artifacts/release/codecat-0.31-win-x64.msi
```

The MSI installs `Codecat.exe` into `Program Files\Codecat` and adds that folder to the system `PATH`.

After installing the MSI, open a new terminal and run:

```powershell
codecat --help
```

Because Windows resolves executables case-insensitively, the installed `Codecat.exe` can be launched as `codecat` from any directory.

Install on Linux for the current user:

```bash
scripts/install-linux.sh --user
```

This publishes a Native AOT `linux-x64` build and installs it as `~/.local/bin/codecat`. If that directory is not on `PATH`, the script adds it to your shell profile. Use `--no-modify-profile` to skip that step.

Run the script without arguments to use an interactive install/uninstall menu:

```bash
scripts/install-linux.sh
```

Install globally on Linux:

```bash
scripts/install-linux.sh --global
```

This installs `codecat` into `/usr/local/bin` and uses `sudo` when needed. Use `--install-dir` to override the target directory or `--runtime` to publish a different Linux runtime identifier.

Uninstall on Linux:

```bash
scripts/install-linux.sh --uninstall --user
scripts/install-linux.sh --uninstall --global
```

Common options:

```powershell
codecat .                         # scan the current directory
codecat . -o context.txt           # choose output file
codecat . --quiet                  # suppress progress output
codecat . --verbose                # print detailed skip information
codecat . --max-file-bytes 250000  # skip larger files
codecat . --mini                   # compact output plus safe minification
codecat . --all                    # include broader optional source/docs files
codecat . --use-gitignore          # also apply .gitignore exclusions
codecat . --no-copy                # write output without copying it to the clipboard
codecat --env-probe                # show OS/terminal detection and copy strategy order
codecat --list-plugins             # show built-in plugin rules
codecat --version
```

After writing the output file, `codecat` automatically copies it to the system clipboard. Use `--no-copy` to disable this. Clipboard copy first uses tmux/OSC 52 when running in SSH or tmux, so remote sessions can copy into the local terminal clipboard. Local fallback tools are `clip.exe` on Windows, `pbcopy` on macOS, and `wl-copy`, `xclip`, or `xsel` on Linux.

Use `codecat --env-probe` to see the detected OS, Linux distribution, terminal, tmux/SSH state, and clipboard strategy order. For SSH inside tmux, tmux and the local terminal must allow OSC 52 clipboard forwarding.

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

<<<FILE path="Program.cs" plugin="csharp" lang="csharp" reason="extension:.cs" lines="80" bytes="2048" sha256="...">>>
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

## Mini Output

`--mini` switches to a compact container format and runs only registered safe content minifiers. If a language has no minifier, file content is preserved apart from output line-ending normalization.

Currently minified languages:

```text
json
xml
css
scss
sass
less
```

Mini output shape:

```text
CC1|root=D:\Git\my-app|files=2|lines=42|bytes=2048|seen=10|skipped=8|warnings=0
F|appsettings.json|config|json|1|120|8|240|m|extension:.json
{"Logging":{"LogLevel":{"Default":"Information"}}}
E
F|src/App.cs|csharp|csharp|40|1928|40|1928|-|extension:.cs
...
E
S|no_plugin_match=8
```

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

See [CHANGELOG.md](CHANGELOG.md) for release history.

## Repository Layout

```text
installer/wix/  Windows MSI definition
scripts/        release/build scripts
  install-linux.sh  Linux install/uninstall helper for per-user and global installs
src/Codecat/
  Cli/        command-line parsing
  Output/     concat.txt writer
  Plugins/    built-in language/ecosystem rules
  Scanning/   recursive project scanner
```

## License

MIT
