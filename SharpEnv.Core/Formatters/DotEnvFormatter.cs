using SharpEnv.Core.Models;

namespace SharpEnv.Core.Formatters;

public sealed class DotEnvFormatter : IEnvFormatter
{
    public string DisplayName => ".env";
    public string DefaultFileName => ".env";
    public string FileFilter => "Env File (*.env)|*.env|All Files (*.*)|*.*";

    public string Format(IEnumerable<ConfigEntry> entries)
    {
        var lines = entries
            .Where(e => e.IsSelected)
            .Select(e => $"{e.Key}=\"{EscapeDotEnv(e.Value)}\"");

        return string.Join(Environment.NewLine, lines);
    }

    private static string EscapeDotEnv(string value) =>
        value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
}
