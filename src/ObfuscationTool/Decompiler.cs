using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Metadata;

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

        var resolver = new UniversalAssemblyResolver(assemblyPath, false, null);
        var peFile = new PEFile(assemblyPath);
        var decompiler = new WholeProjectDecompiler(
            settings,
            resolver,
            new AssemblyReferenceClassifier(),
            DebugInfoProvider.Empty);
        decompiler.DecompileProject(peFile, outputDir);
    }
}
