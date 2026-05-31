# Changelog

All notable changes to `codecat` are documented in this file.

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

[0.2.1]: https://github.com/ppotepa/codecat/releases/tag/v0.2.1
[0.2.0]: https://github.com/ppotepa/codecat/releases/tag/v0.2.0
[0.1.0]: https://github.com/ppotepa/codecat/releases/tag/v0.1.0
