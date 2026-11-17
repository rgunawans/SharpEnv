using SharpEnv.Core.Formatters;
using SharpEnv.Core.Parsers;
using SharpEnv.Core.Services;

namespace SharpEnv.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Explicit registration without reflection for trimming/obfuscation compatibility.
        IConfigParser[] parsers =
        [
            new JsonConfigParser(),
            new IniConfigParser(),
            new CSharpHardcodedParser()
        ];

        IEnvFormatter[] formatters =
        [
            new BashFormatter(),
            new PowerShellFormatter(),
            new CmdFormatter(),
            new DotEnvFormatter(),
            new DockerYamlFormatter()
        ];

        var scannerService = new ProjectScannerService(parsers);
        var initialPaths = args.Where(p => Directory.Exists(p) || File.Exists(p)).ToArray();
        Application.Run(new MainForm(scannerService, formatters, initialPaths));
    }
}
