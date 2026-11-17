using SharpEnv.Core.Models;

namespace SharpEnv.Core.Parsers;

public sealed class IniConfigParser : IConfigParser
{
    public ConfigSourceType SourceType => ConfigSourceType.Ini;

    public bool CanParse(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".ini", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<ConfigEntry> Parse(string content, string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var entries = new List<ConfigEntry>();
        string? currentSection = null;

        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) ||
                trimmed.StartsWith(';') ||
                trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']') && trimmed.Length >= 2)
            {
                currentSection = trimmed[1..^1].Trim();
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var rawKey = trimmed[..separatorIndex].Trim();
            if (string.IsNullOrEmpty(rawKey))
            {
                continue;
            }

            var rawValue = Unquote(trimmed[(separatorIndex + 1)..].Trim());
            var fullKey = string.IsNullOrEmpty(currentSection)
                ? rawKey
                : $"{currentSection}__{rawKey}";

            entries.Add(new ConfigEntry
            {
                Key = fullKey,
                Value = rawValue,
                SourceType = ConfigSourceType.Ini,
                RelativeFilePath = relativeFilePath,
                IsSelected = true
            });
        }

        return entries;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) ||
             (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            return value[1..^1];
        }

        return value;
    }
}
