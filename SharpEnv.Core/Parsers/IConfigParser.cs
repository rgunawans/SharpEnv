using SharpEnv.Core.Models;

namespace SharpEnv.Core.Parsers;

public interface IConfigParser
{
    ConfigSourceType SourceType { get; }
    bool CanParse(string filePath);
    IEnumerable<ConfigEntry> Parse(string content, string relativeFilePath);
}
