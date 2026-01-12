using System.Diagnostics;

namespace ObfuscationTool;

internal static class Program
{
    private const int SuccessCode = 0;
    private const int ErrorCode = 1;

    public static int Main(string[] args)
    {
        try
        {
            var options = Options.Parse(args);
            if (options is null)
            {
                Options.PrintUsage();
                return ErrorCode;
            }

            var slnPath = Path.GetFullPath(options.SolutionPath);
            var outputDir = Path.GetFullPath(options.OutputDirectory);
            var configuration = options.Configuration;

            if (!File.Exists(slnPath))
            {
                Console.Error.WriteLine($"Solution not found: {slnPath}");
                return ErrorCode;
            }

            Directory.CreateDirectory(outputDir);

            Console.WriteLine("Building solution...");
            ProcessRunner.Run("dotnet", $"build \"{slnPath}\" -c {configuration}");

            var projectPaths = SlnParser.GetProjectPaths(slnPath).ToList();
            if (projectPaths.Count == 0)
            {
                Console.Error.WriteLine("No C# projects found in solution.");
                return ErrorCode;
            }

            var obfuscatedAssemblies = new List<string>();
            foreach (var projectPath in projectPaths)
            {
                var fullProjectPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(slnPath)!, projectPath));
                if (!File.Exists(fullProjectPath))
                {
                    Console.Error.WriteLine($"Project not found: {fullProjectPath}");
                    continue;
                }

                var targetPath = MsBuildInspector.GetTargetPath(fullProjectPath, configuration);
                if (string.IsNullOrWhiteSpace(targetPath) || !File.Exists(targetPath))
                {
                    Console.Error.WriteLine($"Target assembly not found for project: {fullProjectPath}");
                    continue;
                }

                var obfuscatedAssemblyPath = Obfuscator.ObfuscateAssembly(targetPath, outputDir);
                if (!string.IsNullOrWhiteSpace(obfuscatedAssemblyPath))
                {
                    obfuscatedAssemblies.Add(obfuscatedAssemblyPath);
                }
            }

            if (obfuscatedAssemblies.Count == 0)
            {
                Console.Error.WriteLine("No assemblies were obfuscated.");
                return ErrorCode;
            }

            Console.WriteLine("Decompiling obfuscated assemblies to C#...");
            foreach (var assemblyPath in obfuscatedAssemblies)
            {
                Decompiler.DecompileAssembly(assemblyPath, outputDir);
            }

            Console.WriteLine($"Obfuscated sources saved to: {outputDir}");
            return SuccessCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(ex);
            return ErrorCode;
        }
    }
}

internal sealed record Options(string SolutionPath, string OutputDirectory, string Configuration)
{
    public static Options? Parse(string[] args)
    {
        string? sln = null;
        string? output = null;
        string configuration = "Release";

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--sln" when i + 1 < args.Length:
                    sln = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    output = args[++i];
                    break;
                case "--configuration" when i + 1 < args.Length:
                    configuration = args[++i];
                    break;
                case "--help":
                case "-h":
                case "-?":
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(sln) || string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        return new Options(sln, output, configuration);
    }

    public static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  ObfuscationTool --sln <path to .sln> --out <output directory> [--configuration Release]");
    }
}

internal static class ProcessRunner
{
    public static void Run(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process is null)
        {
            throw new InvalidOperationException($"Failed to start process: {fileName}");
        }

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) Console.Error.WriteLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command failed: {fileName} {arguments}");
        }
    }

    public static string RunAndCapture(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process is null)
        {
            throw new InvalidOperationException($"Failed to start process: {fileName}");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command failed: {fileName} {arguments}\n{error}");
        }

        return output;
    }
}

internal static class MsBuildInspector
{
    public static string? GetTargetPath(string projectPath, string configuration)
    {
        var output = ProcessRunner.RunAndCapture(
            "dotnet",
            $"msbuild \"{projectPath}\" -getProperty:TargetPath -property:Configuration={configuration}");

        var lines = output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return lines.LastOrDefault();
    }
}
