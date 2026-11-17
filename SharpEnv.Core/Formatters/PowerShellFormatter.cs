using SharpEnv.Core.Models;

namespace SharpEnv.Core.Formatters;

public sealed class PowerShellFormatter : IEnvFormatter
{
    public string DisplayName => "PowerShell";
    public string DefaultFileName => "env.ps1";
    public string FileFilter => "PowerShell Script (*.ps1)|*.ps1|All Files (*.*)|*.*";

    public string Format(IEnumerable<ConfigEntry> entries)
    {
        var lines = entries
            .Where(e => e.IsSelected)
            .Select(e => $"$env:{e.Key}=\"{EscapePowerShell(e.Value)}\"");

        return string.Join(Environment.NewLine, lines);
    }

    private static string EscapePowerShell(string value) =>
        value
            .Replace("`", "``")
            .Replace("\"", "`\"")
            .Replace("$", "`$");
}
