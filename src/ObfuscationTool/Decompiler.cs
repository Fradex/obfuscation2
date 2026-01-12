using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;

namespace ObfuscationTool;

internal static class Decompiler
{
    public static void DecompileAssembly(string assemblyPath, string outputDirectory)
    {
        var outputDir = Path.Combine(outputDirectory, "sources", Path.GetFileNameWithoutExtension(assemblyPath));
        Directory.CreateDirectory(outputDir);

        var settings = new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false,
            RemoveDeadCode = false,
            UseDebugSymbols = false
        };

        var decompiler = new WholeProjectDecompiler(settings);
        decompiler.DecompileProject(assemblyPath, outputDir);
    }
}
