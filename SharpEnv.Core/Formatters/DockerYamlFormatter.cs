using SharpEnv.Core.Models;

namespace SharpEnv.Core.Formatters;

public sealed class DockerYamlFormatter : IEnvFormatter
{
    public string DisplayName => "Docker YAML";
    public string DefaultFileName => "docker-env.yml";
    public string FileFilter => "YAML File (*.yml;*.yaml)|*.yml;*.yaml|All Files (*.*)|*.*";

    public string Format(IEnumerable<ConfigEntry> entries)
    {
        var lines = entries
            .Where(e => e.IsSelected)
            .Select(e => $"      - {e.Key}={e.Value}");

        return string.Join(Environment.NewLine, lines);
    }
}
