# Working in Codecat

## Projects

- `Codecat.Core` contains shared models and interfaces.
- `Codecat.Plugins` contains compiled built-in plugin definitions and the shared matcher.
- `Codecat.Application` coordinates scans and applies scan policy. It must not reference Infrastructure.
- `Codecat.Infrastructure` implements filesystem, metrics, and minifier services against Core contracts.
- `Codecat.Cli` parses commands, composes services, formats output, and owns clipboard interaction.
- `tests/Codecat.Tests` covers cross-project behavior and regressions.

The production entry point is `src/Codecat.Cli/Program.cs`. There is one active implementation of the application.

## Common changes

- Add a language in `src/Codecat.Plugins/BuiltIns/`, then register it once in `BuiltInPluginCatalog` and add a focused test.
- Change traversal or filtering in `Codecat.Application/Scanning` and preserve the reason reported for skipped items.
- Change file decoding or filesystem access in Infrastructure behind the Core interfaces.
- Change output or clipboard behavior in `Codecat.Cli`; writers must not perform clipboard operations.
- Update README behavior and examples when changing user-visible CLI behavior.

## Checks

Run `dotnet test Codecat.slnx --configuration Release` before completing a code change. Native AOT publishing is a separate release check.
