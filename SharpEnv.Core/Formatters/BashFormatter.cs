using SharpEnv.Core.Models;

namespace SharpEnv.Core.Formatters;

public sealed class BashFormatter : IEnvFormatter
{
    public string DisplayName => "Linux Bash";
    public string DefaultFileName => "env.sh";
    public string FileFilter => "Shell Script (*.sh)|*.sh|All Files (*.*)|*.*";

    public string Format(IEnumerable<ConfigEntry> entries)
    {
        var lines = entries
            .Where(e => e.IsSelected)
            .Select(e => $"export {e.Key}='{EscapeBashSingleQuoted(e.Value)}'");

        return string.Join(Environment.NewLine, lines);
    }

    private static string EscapeBashSingleQuoted(string value) =>
        value.Replace("'", "'\\''");
}
