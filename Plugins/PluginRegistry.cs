namespace Codecat.Plugins;

internal static class PluginRegistry
{
    public static IReadOnlyList<ICodecatPlugin> CreateDefault()
    {
        var ignored = new[]
        {
            ".git", ".hg", ".svn", ".vs", ".vscode", ".idea",
            "bin", "obj", "build", "dist", "out", "target", "coverage",
            "node_modules", ".next", ".nuxt", ".svelte-kit", ".parcel-cache",
            ".gradle", ".cxx", "CMakeFiles", "cmake-build-debug", "cmake-build-release",
            ".venv", "venv", "__pycache__", ".pytest_cache", ".mypy_cache", ".ruff_cache",
            ".build", "DerivedData", ".dart_tool", ".luarocks", ".Rproj.user",
            ".bsp", ".metals", "_build", "deps", ".stack-work", "dist-newstyle",
            ".bundle", "vendor", ".docusaurus", "_site", "site"
        };

        return
        [
            new ExtensionPlugin(
                "csharp",
                new Dictionary<string, string>
                {
                    [".cs"] = "csharp",
                    [".csproj"] = "xml",
                    [".sln"] = "text",
                    [".slnx"] = "xml",
                    [".props"] = "xml",
                    [".targets"] = "xml"
                },
                ignoredDirectories: ignored),

            new ExtensionPlugin(
                "java",
                new Dictionary<string, string>
                {
                    [".java"] = "java",
                    [".jsp"] = "jsp"
                },
                new Dictionary<string, string>
                {
                    ["pom.xml"] = "xml",
                    ["mvnw"] = "shell",
                    ["mvnw.cmd"] = "batch"
                },
                ignored),

            new ExtensionPlugin(
                "kotlin",
                new Dictionary<string, string>
                {
                    [".kt"] = "kotlin",
                    [".kts"] = "kotlin"
                },
                ignoredDirectories: ignored),

            new ExtensionPlugin(
                "android",
                new Dictionary<string, string>
                {
                    [".aidl"] = "aidl",
                    [".gradle"] = "gradle"
                },
                new Dictionary<string, string>
                {
                    ["AndroidManifest.xml"] = "xml",
                    ["settings.gradle"] = "gradle",
                    ["settings.gradle.kts"] = "kotlin",
                    ["build.gradle"] = "gradle",
                    ["build.gradle.kts"] = "kotlin",
                    ["gradle.properties"] = "properties",
                    ["proguard-rules.pro"] = "proguard"
                },
                ignored),

            new ExtensionPlugin(
                "swift",
                new Dictionary<string, string> { [".swift"] = "swift" },
                new Dictionary<string, string> { ["Package.swift"] = "swift" },
                ignored),

            new ExtensionPlugin(
                "objective-c",
                new Dictionary<string, string>
                {
                    [".m"] = "objective-c",
                    [".mm"] = "objective-cpp"
                },
                ignoredDirectories: ignored),

            new ExtensionPlugin(
                "cpp",
                new Dictionary<string, string>
                {
                    [".cc"] = "cpp",
                    [".cpp"] = "cpp",
                    [".cxx"] = "cpp",
                    [".hh"] = "cpp",
                    [".hpp"] = "cpp",
                    [".hxx"] = "cpp",
                    [".ixx"] = "cpp",
                    [".cmake"] = "cmake"
                },
                new Dictionary<string, string> { ["CMakeLists.txt"] = "cmake" },
                ignored),

            new ExtensionPlugin(
                "c",
                new Dictionary<string, string>
                {
                    [".c"] = "c",
                    [".h"] = "c"
                },
                new Dictionary<string, string> { ["Makefile"] = "makefile" },
                ignored),

            new ExtensionPlugin(
                "rust",
                new Dictionary<string, string> { [".rs"] = "rust" },
                new Dictionary<string, string>
                {
                    ["Cargo.toml"] = "toml",
                    ["Cargo.lock"] = "toml"
                },
                ignored),

            new ExtensionPlugin(
                "go",
                new Dictionary<string, string> { [".go"] = "go" },
                new Dictionary<string, string>
                {
                    ["go.mod"] = "gomod",
                    ["go.sum"] = "gosum",
                    ["go.work"] = "gowork"
                },
                ignored),

            new ExtensionPlugin(
                "python",
                new Dictionary<string, string>
                {
                    [".py"] = "python",
                    [".pyi"] = "python"
                },
                new Dictionary<string, string>
                {
                    ["pyproject.toml"] = "toml",
                    ["requirements.txt"] = "requirements",
                    ["requirements-dev.txt"] = "requirements",
                    ["Pipfile"] = "toml",
                    ["Pipfile.lock"] = "json",
                    ["poetry.lock"] = "toml",
                    ["setup.py"] = "python"
                },
                ignored),

            new ExtensionPlugin(
                "javascript",
                new Dictionary<string, string>
                {
                    [".js"] = "javascript",
                    [".jsx"] = "javascript",
                    [".mjs"] = "javascript",
                    [".cjs"] = "javascript"
                },
                new Dictionary<string, string>
                {
                    ["package.json"] = "json",
                    ["package-lock.json"] = "json",
                    ["vite.config.js"] = "javascript",
                    ["next.config.js"] = "javascript",
                    ["webpack.config.js"] = "javascript"
                },
                ignored),

            new ExtensionPlugin(
                "typescript",
                new Dictionary<string, string>
                {
                    [".ts"] = "typescript",
                    [".tsx"] = "typescript",
                    [".mts"] = "typescript",
                    [".cts"] = "typescript"
                },
                new Dictionary<string, string>
                {
                    ["tsconfig.json"] = "json",
                    ["vite.config.ts"] = "typescript",
                    ["next.config.ts"] = "typescript"
                },
                ignored),

            new ExtensionPlugin(
                "web",
                new Dictionary<string, string>
                {
                    [".html"] = "html",
                    [".htm"] = "html",
                    [".css"] = "css",
                    [".scss"] = "scss",
                    [".sass"] = "sass",
                    [".less"] = "less",
                    [".svg"] = "svg"
                },
                ignoredDirectories: ignored),

            new ExtensionPlugin(
                "php",
                new Dictionary<string, string> { [".php"] = "php", [".phtml"] = "php" },
                new Dictionary<string, string>
                {
                    ["composer.json"] = "json",
                    ["composer.lock"] = "json"
                },
                ignored),

            new ExtensionPlugin(
                "ruby",
                new Dictionary<string, string>
                {
                    [".rb"] = "ruby",
                    [".erb"] = "erb",
                    [".gemspec"] = "ruby"
                },
                new Dictionary<string, string>
                {
                    ["Gemfile"] = "ruby",
                    ["Gemfile.lock"] = "text",
                    ["Rakefile"] = "ruby"
                },
                ignored),

            new ExtensionPlugin(
                "dart-flutter",
                new Dictionary<string, string> { [".dart"] = "dart" },
                new Dictionary<string, string>
                {
                    ["pubspec.yaml"] = "yaml",
                    ["pubspec.lock"] = "yaml",
                    ["analysis_options.yaml"] = "yaml"
                },
                ignored),

            new ExtensionPlugin(
                "lua",
                new Dictionary<string, string>
                {
                    [".lua"] = "lua",
                    [".rockspec"] = "lua"
                },
                ignoredDirectories: ignored),

            new ExtensionPlugin(
                "r",
                new Dictionary<string, string>
                {
                    [".r"] = "r",
                    [".R"] = "r",
                    [".Rmd"] = "rmarkdown"
                },
                new Dictionary<string, string>
                {
                    ["DESCRIPTION"] = "text",
                    ["NAMESPACE"] = "text"
                },
                ignored),

            new ExtensionPlugin(
                "julia",
                new Dictionary<string, string> { [".jl"] = "julia" },
                new Dictionary<string, string>
                {
                    ["Project.toml"] = "toml",
                    ["Manifest.toml"] = "toml"
                },
                ignored),

            new ExtensionPlugin(
                "scala",
                new Dictionary<string, string>
                {
                    [".scala"] = "scala",
                    [".sbt"] = "scala"
                },
                new Dictionary<string, string> { ["build.sbt"] = "scala" },
                ignored),

            new ExtensionPlugin(
                "elixir",
                new Dictionary<string, string>
                {
                    [".ex"] = "elixir",
                    [".exs"] = "elixir"
                },
                new Dictionary<string, string>
                {
                    ["mix.exs"] = "elixir",
                    ["mix.lock"] = "elixir"
                },
                ignored),

            new ExtensionPlugin(
                "erlang",
                new Dictionary<string, string>
                {
                    [".erl"] = "erlang",
                    [".hrl"] = "erlang",
                    [".app.src"] = "erlang"
                },
                new Dictionary<string, string> { ["rebar.config"] = "erlang" },
                ignored),

            new ExtensionPlugin(
                "haskell",
                new Dictionary<string, string>
                {
                    [".hs"] = "haskell",
                    [".lhs"] = "haskell",
                    [".cabal"] = "cabal"
                },
                new Dictionary<string, string>
                {
                    ["stack.yaml"] = "yaml",
                    ["package.yaml"] = "yaml"
                },
                ignored),

            new ExtensionPlugin(
                "fsharp",
                new Dictionary<string, string>
                {
                    [".fs"] = "fsharp",
                    [".fsi"] = "fsharp",
                    [".fsx"] = "fsharp",
                    [".fsproj"] = "xml"
                },
                ignoredDirectories: ignored),

            new ExtensionPlugin(
                "powershell",
                new Dictionary<string, string>
                {
                    [".ps1"] = "powershell",
                    [".psm1"] = "powershell",
                    [".psd1"] = "powershell"
                },
                ignoredDirectories: ignored),

            new ExtensionPlugin(
                "shell",
                new Dictionary<string, string>
                {
                    [".sh"] = "shell",
                    [".bash"] = "shell",
                    [".zsh"] = "shell",
                    [".fish"] = "fish",
                    [".bat"] = "batch",
                    [".cmd"] = "batch"
                },
                new Dictionary<string, string>
                {
                    ["Dockerfile"] = "dockerfile",
                    ["Containerfile"] = "dockerfile",
                    ["Makefile"] = "makefile"
                },
                ignored),

            new ExtensionPlugin(
                "sql",
                new Dictionary<string, string>
                {
                    [".sql"] = "sql",
                    [".psql"] = "sql",
                    [".mysql"] = "sql"
                },
                ignoredDirectories: ignored),

            new ExtensionPlugin(
                "config",
                new Dictionary<string, string>
                {
                    [".json"] = "json",
                    [".yaml"] = "yaml",
                    [".yml"] = "yaml",
                    [".toml"] = "toml",
                    [".ini"] = "ini",
                    [".conf"] = "conf",
                    [".config"] = "xml",
                    [".editorconfig"] = "editorconfig",
                    [".gitignore"] = "gitignore",
                    [".gitattributes"] = "gitattributes"
                },
                new Dictionary<string, string>
                {
                    [".env.example"] = "dotenv",
                    ["appsettings.json"] = "json",
                    ["appsettings.Development.json"] = "json"
                },
                ignored),

            new ExtensionPlugin(
                "docs",
                new Dictionary<string, string>
                {
                    [".md"] = "markdown",
                    [".mdx"] = "mdx",
                    [".rst"] = "rst",
                    [".txt"] = "text",
                    [".adoc"] = "asciidoc"
                },
                new Dictionary<string, string>
                {
                    ["README"] = "text",
                    ["LICENSE"] = "text",
                    ["CHANGELOG"] = "text",
                    ["CHANGELOG.md"] = "markdown"
                },
                ignored)
        ];
    }
}
