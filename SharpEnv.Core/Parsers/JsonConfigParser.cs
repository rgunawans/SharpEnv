using System.Text.Json;
using SharpEnv.Core.Models;

namespace SharpEnv.Core.Parsers;

public sealed class JsonConfigParser : IConfigParser
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public ConfigSourceType SourceType => ConfigSourceType.Json;

    public bool CanParse(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".json", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<ConfigEntry> Parse(string content, string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var entries = new List<ConfigEntry>();

        try
        {
            using var document = JsonDocument.Parse(content, DocumentOptions);
            VisitElement(document.RootElement, string.Empty, relativeFilePath, entries);
        }
        catch (JsonException)
        {
            // Skip malformed JSON files without aborting the entire project scan.
        }

        return entries;
    }

    private static void VisitElement(
        JsonElement element,
        string prefix,
        string relativeFilePath,
        List<ConfigEntry> entries)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}__{property.Name}";
                    VisitElement(property.Value, key, relativeFilePath, entries);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? index.ToString()
                        : $"{prefix}__{index}";
                    VisitElement(item, key, relativeFilePath, entries);
                    index++;
                }
                break;

            case JsonValueKind.String:
                AddEntry(prefix, element.GetString() ?? string.Empty, relativeFilePath, entries);
                break;

            case JsonValueKind.Number:
                AddEntry(prefix, element.GetRawText(), relativeFilePath, entries);
                break;

            case JsonValueKind.True:
                AddEntry(prefix, "true", relativeFilePath, entries);
                break;

            case JsonValueKind.False:
                AddEntry(prefix, "false", relativeFilePath, entries);
                break;

            case JsonValueKind.Null:
                AddEntry(prefix, string.Empty, relativeFilePath, entries);
                break;
        }
    }

    private static void AddEntry(
        string key,
        string value,
        string relativeFilePath,
        List<ConfigEntry> entries)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        entries.Add(new ConfigEntry
        {
            Key = key,
            Value = value,
            SourceType = ConfigSourceType.Json,
            RelativeFilePath = relativeFilePath,
            IsSelected = true
        });
    }
}
