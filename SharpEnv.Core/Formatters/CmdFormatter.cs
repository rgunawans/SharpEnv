using SharpEnv.Core.Models;

namespace SharpEnv.Core.Formatters;

public sealed class CmdFormatter : IEnvFormatter
{
    public string DisplayName => "Windows CMD";
    public string DefaultFileName => "env.bat";
    public string FileFilter => "Batch File (*.bat;*.cmd)|*.bat;*.cmd|All Files (*.*)|*.*";

    public string Format(IEnumerable<ConfigEntry> entries)
    {
        var lines = entries
            .Where(e => e.IsSelected)
            .Select(e => $"set {e.Key}={e.Value}");

        return string.Join(Environment.NewLine, lines);
    }
}
