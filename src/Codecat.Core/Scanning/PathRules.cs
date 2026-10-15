namespace Codecat.Scanning;

public static class PathRules
{
    public static StringComparer Comparer => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}
