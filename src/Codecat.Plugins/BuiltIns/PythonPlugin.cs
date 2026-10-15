using Codecat.Plugins;

namespace Codecat.Plugins.BuiltIns;

internal static class PythonPlugin
{
    internal static ICodecatPlugin Create()
    {
        return new ExtensionPlugin(
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
                });
    }
}
