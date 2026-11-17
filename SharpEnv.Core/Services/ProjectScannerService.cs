using SharpEnv.Core.Models;
using SharpEnv.Core.Parsers;

namespace SharpEnv.Core.Services;

public sealed class ProjectScannerService
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        ".git",
        ".vs",
        ".idea",
        "node_modules",
        "packages"
    };

    private readonly IReadOnlyList<IConfigParser> _parsers;

    public ProjectScannerService(IEnumerable<IConfigParser> parsers)
    {
        _parsers = parsers.ToList();
    }

    public Task<List<ConfigEntry>> ScanAsync(
        string targetPath,
        ScanOptions options,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentNullException.ThrowIfNull(options);

        return Task.Run(async () =>
        {
            var results = new List<ConfigEntry>();

            if (File.Exists(targetPath))
            {
                var fullFilePath = Path.GetFullPath(targetPath);
                var relativePath = Path.GetFileName(fullFilePath);
                var projectCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var projectName = ResolveProjectName(
                    Path.GetDirectoryName(fullFilePath),
                    rootBoundary: null,
                    defaultName: "Standalone Files",
                    projectCache);

                await ParseFileAsync(fullFilePath, relativePath, projectName, options, results, ct).ConfigureAwait(false);
                return results;
            }

            if (!Directory.Exists(targetPath))
            {
                throw new DirectoryNotFoundException($"Path not found: {targetPath}");
            }

            var rootPath = Path.GetFullPath(targetPath);
            var dirProjectCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var filePath in EnumerateProjectFiles(rootPath, ct))
            {
                ct.ThrowIfCancellationRequested();
                var relativePath = Path.GetRelativePath(rootPath, filePath);
                var projectName = ResolveProjectName(
                    Path.GetDirectoryName(filePath),
                    rootPath,
                    defaultName: "Solution / Root",
                    dirProjectCache);

                await ParseFileAsync(filePath, relativePath, projectName, options, results, ct).ConfigureAwait(false);
            }

            return results;
        }, ct);
    }

    private async Task ParseFileAsync(
        string filePath,
        string relativePath,
        string projectName,
        ScanOptions options,
        List<ConfigEntry> results,
        CancellationToken ct)
    {
        var parser = FindEnabledParser(filePath, options);
        if (parser is null)
        {
            return;
        }

        var content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        foreach (var entry in parser.Parse(content, relativePath))
        {
            entry.ProjectName = projectName;
            results.Add(entry);
        }
    }

    private static string ResolveProjectName(
        string? startDirectory,
        string? rootBoundary,
        string defaultName,
        Dictionary<string, string> cache)
    {
        if (string.IsNullOrEmpty(startDirectory))
        {
            return defaultName;
        }

        if (cache.TryGetValue(startDirectory, out var cached))
        {
            return cached;
        }

        var visited = new List<string>();
        var current = startDirectory;
        string? resolved = null;

        while (!string.IsNullOrEmpty(current))
        {
            if (cache.TryGetValue(current, out var existing))
            {
                resolved = existing;
                break;
            }

            visited.Add(current);

            var projectFile = FindProjectFileInDirectory(current);
            if (projectFile is not null)
            {
                resolved = Path.GetFileNameWithoutExtension(projectFile);
                break;
            }

            if (rootBoundary is not null &&
                string.Equals(current, rootBoundary, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current = Path.GetDirectoryName(current);
        }

        resolved ??= defaultName;
        foreach (var dir in visited)
        {
            cache[dir] = resolved;
        }

        return resolved;
    }

    private static string? FindProjectFileInDirectory(string directory)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.*proj"))
            {
                var ext = Path.GetExtension(file);
                if (ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".fsproj", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".vbproj", StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore inaccessible directories
        }

        return null;
    }

    private IConfigParser? FindEnabledParser(string filePath, ScanOptions options)
    {
        foreach (var parser in _parsers)
        {
            if (!IsSourceEnabled(parser.SourceType, options))
            {
                continue;
            }

            if (parser.CanParse(filePath))
            {
                return parser;
            }
        }

        return null;
    }

    private static bool IsSourceEnabled(ConfigSourceType sourceType, ScanOptions options) =>
        sourceType switch
        {
            ConfigSourceType.Json => options.IncludeJson,
            ConfigSourceType.Ini => options.IncludeIni,
            ConfigSourceType.CSharp => options.IncludeCSharp,
            _ => false
        };

    private static IEnumerable<string> EnumerateProjectFiles(string rootDirectory, CancellationToken ct)
    {
        var pending = new Stack<string>();
        pending.Push(rootDirectory);

        while (pending.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var currentDir = pending.Pop();

            string[] subDirs;
            try
            {
                subDirs = Directory.GetDirectories(currentDir);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var subDir in subDirs)
            {
                var dirName = Path.GetFileName(subDir);
                if (!IgnoredDirectories.Contains(dirName))
                {
                    pending.Push(subDir);
                }
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(currentDir);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }
        }
    }
}
