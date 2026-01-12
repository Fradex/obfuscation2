using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ObfuscationTool;

internal static class Obfuscator
{
    public static string? ObfuscateAssembly(string assemblyPath, string outputDirectory)
    {
        if (!File.Exists(assemblyPath))
        {
            return null;
        }

        var outputDir = Path.Combine(outputDirectory, "assemblies");
        Directory.CreateDirectory(outputDir);

        var outputPath = Path.Combine(outputDir, Path.GetFileName(assemblyPath));
        var readerParameters = new ReaderParameters
        {
            ReadSymbols = false
        };

        var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
        foreach (var type in assembly.MainModule.Types)
        {
            ObfuscateType(type);
        }

        assembly.Write(outputPath);
        return outputPath;
    }

    private static void ObfuscateType(TypeDefinition type)
    {
        foreach (var method in type.Methods)
        {
            if (!method.HasBody || method.IsAbstract || method.Body.Instructions.Count == 0)
            {
                continue;
            }

            InsertOpaquePredicate(method);
        }

        foreach (var nested in type.NestedTypes)
        {
            ObfuscateType(nested);
        }
    }

    private static void InsertOpaquePredicate(MethodDefinition method)
    {
        var il = method.Body.GetILProcessor();
        var first = method.Body.Instructions[0];

        var tempVariable = new VariableDefinition(method.Module.TypeSystem.Boolean);
        method.Body.Variables.Add(tempVariable);
        method.Body.InitLocals = true;

        var instructions = new List<Instruction>
        {
            il.Create(OpCodes.Ldc_I4_0),
            il.Create(OpCodes.Stloc, tempVariable),
            il.Create(OpCodes.Ldloc, tempVariable),
            il.Create(OpCodes.Brtrue_S, first),
            il.Create(OpCodes.Nop)
        };

        foreach (var instruction in instructions)
        {
            il.InsertBefore(first, instruction);
        }
    }
}
