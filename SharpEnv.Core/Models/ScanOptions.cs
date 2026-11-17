namespace SharpEnv.Core.Models;

public sealed class ScanOptions
{
    public bool IncludeJson { get; set; } = true;
    public bool IncludeIni { get; set; } = true;
    public bool IncludeCSharp { get; set; } = true;
}
