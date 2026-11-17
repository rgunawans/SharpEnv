using SharpEnv.Core.Models;

namespace SharpEnv.Core.Formatters;

public interface IEnvFormatter
{
    string DisplayName { get; }
    string DefaultFileName { get; }
    string FileFilter { get; }
    string Format(IEnumerable<ConfigEntry> entries);
}
