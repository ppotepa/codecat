# Changelog

All notable changes to `codecat` are documented in this file.

## [0.36] - 2026-06-28

### Added

- Added `--zip` to write the generated text output into a maximum-compression ZIP archive.

## [0.35] - 2026-06-28

### Added

- Added WGSL shader file detection through a dedicated `shader` plugin.
- Added Rhai script file detection through a dedicated `script` plugin.
- Added generic TOML file detection through a dedicated `toml` plugin.

## [0.34] - 2026-06-22

### Added

- Added support for filtering scanned files by extension via `--extensions`, including bracket syntax `[ext, ext, ...]`.
- Added a dedicated Gradle plugin for Groovy and Kotlin DSL build files, Gradle wrapper properties, and version catalogs.
- Expanded Flutter/Dart, Android/NDK, C++/CMake, Rust tooling, PowerShell, Git submodule, HTTP/API, GraphQL, and CodeGraph configuration file detection.
- Added binary artifact exclusions for Android packages, JVM bytecode, native object/static libraries, and local index databases.

### Changed

- Split the scanner into Core, Application, Infrastructure, and CLI projects.
- Updated Windows release build automation to publish the new CLI project and upload release assets.

## [0.33] - 2026-06-13

### Added

- Added a Linux install/uninstall helper with per-user and global modes.
- Added automatic clipboard copy after writing the output file.
- Added `--no-copy` / `--no-clipboard` to disable clipboard copy.
- Added SSH/tmux clipboard support through tmux and OSC 52.
- Added Linux clipboard support through `wl-copy`, `xclip`, or `xsel`.
- Added `--env-probe` to print OS, terminal, tmux, SSH, and clipboard strategy detection.

### Changed

- Moved clipboard handling out of the writer into a dedicated output component.

## [0.31] - 2026-06-04

### Added

- Added `--all` to include broader optional source/docs files such as Markdown.
- Added `--use-gitignore` / `--gitignore` to exclude paths matched by `.gitignore` files.

### Changed

- Markdown `.md` files are ignored by default unless `--all` is used.

## [0.3.0] - 2026-06-01

### Added

- Added `--mini` compact output mode.
- Added safe content minifier registry.
- Added built-in minifiers for JSON, XML, CSS, SCSS, Sass, and Less.
- Added original line/byte counts and minification status to default file metadata.

## [0.2.1] - 2026-05-31

### Changed

- Refactored the scanner into an allowlist-first pipeline:
  `global deny -> plugin allowlist match -> safety filters -> include`.
- Added `PluginMatch` metadata so each included file records why it matched.
- Added `reason="..."` to each file header in `concat.txt`, for example:
  `reason="extension:.cs"` or `reason="filename:pyproject.toml"`.
- Moved global deny and safety rules into a dedicated scanner rules component.
- Republished Windows ZIP and MSI artifacts from the latest scanner implementation.

## [0.2.0] - 2026-05-31

### Added

- Added scanner diagnostics and progress reporting.
- Added warning collection for readable non-fatal filesystem errors.
- Added scan statistics to `concat.txt`:
  `DIRECTORIES_VISITED`, `FILES_SEEN`, `ITEMS_SKIPPED`, `WARNINGS`, and `SKIPPED_BY_REASON`.
- Added CLI options:
  `--quiet`, `--verbose`, `--max-file-bytes`, `--list-plugins`, and `--version`.
- Added safer exception handling for file and directory enumeration.
- Added stricter default scanning rules:
  hidden directory skipping, binary/asset extension exclusion, previous `codecat` output exclusion, and lower default file size limit.

### Changed

- Reduced default maximum file size from 1 MB to 250 KB.
- Removed SVG from the default web plugin include list.
- Updated Windows release artifacts for the stricter scanner.

## [0.1.0] - 2026-05-31

### Added

- Initial public release of `codecat`.
- Added Native AOT C# CLI for recursively concatenating source/config files into one LLM-friendly text file.
- Added plugin-based scanning with 30 compiled-in plugins:
  C#, Java, Kotlin, Android, Swift, Objective-C, C++, C, Rust, Go, Python, JavaScript, TypeScript, Web, PHP, Ruby, Dart/Flutter, Lua, R, Julia, Scala, Elixir, Erlang, Haskell, F#, PowerShell, Shell, SQL, Config, and Docs.
- Added LLM-friendly output format with global metadata, per-file metadata, SHA-256 hashes, and explicit file delimiters.
- Added Native AOT Windows release build.
- Added Windows MSI installer that installs `Codecat.exe` into `Program Files\Codecat` and adds it to `PATH`.
- Added repository structure under `src/Codecat`, solution file, CI workflow, MIT license, editor config, and release build script.

[0.36]: https://github.com/ppotepa/codecat/releases/tag/v0.36
[0.35]: https://github.com/ppotepa/codecat/releases/tag/v0.35
[0.34]: https://github.com/ppotepa/codecat/releases/tag/v0.34
[0.33]: https://github.com/ppotepa/codecat/releases/tag/v0.33
[0.31]: https://github.com/ppotepa/codecat/releases/tag/v0.31
[0.3.0]: https://github.com/ppotepa/codecat/releases/tag/v0.3.0
[0.2.1]: https://github.com/ppotepa/codecat/releases/tag/v0.2.1
[0.2.0]: https://github.com/ppotepa/codecat/releases/tag/v0.2.0
[0.1.0]: https://github.com/ppotepa/codecat/releases/tag/v0.1.0
