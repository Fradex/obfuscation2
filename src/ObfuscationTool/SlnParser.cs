namespace ObfuscationTool;

internal static class SlnParser
{
    public static IEnumerable<string> GetProjectPaths(string slnPath)
    {
        foreach (var line in File.ReadLines(slnPath))
        {
            if (!line.StartsWith("Project(", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 2)
            {
                continue;
            }

            var pathPart = parts[1].Trim().Trim('"');
            if (pathPart.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                yield return pathPart.Replace('\\', Path.DirectorySeparatorChar);
            }
        }
    }
}
