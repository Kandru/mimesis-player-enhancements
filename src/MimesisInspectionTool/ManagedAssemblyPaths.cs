namespace MimesisInspectionTool;

internal static class ManagedAssemblyPaths
{
    public static string Resolve(string? managedPath, string? gamePath)
    {
        if (!string.IsNullOrWhiteSpace(managedPath))
            return Path.GetFullPath(managedPath);

        if (!string.IsNullOrWhiteSpace(gamePath))
        {
            string fromGame = Path.Combine(gamePath, "MIMESIS_Data", "Managed");
            if (Directory.Exists(fromGame))
                return Path.GetFullPath(fromGame);
        }

        string? envGame = Environment.GetEnvironmentVariable("MIMESIS_PATH");
        if (!string.IsNullOrWhiteSpace(envGame))
        {
            string fromEnv = Path.Combine(envGame, "MIMESIS_Data", "Managed");
            if (Directory.Exists(fromEnv))
                return Path.GetFullPath(fromEnv);
        }

        string repoRoot = FindRepoRoot();
        string bootstrap = Path.Combine(repoRoot, "deps", "reference", "Managed");
        if (Directory.Exists(bootstrap))
            return bootstrap;

        throw new InvalidOperationException(
            "Could not find game assemblies. Pass --managed <path>, --game <MIMESIS root>, set MIMESIS_PATH, " +
            "or run ./scripts/bootstrap-deps.sh from the repo root.");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "deps", "reference", "Managed")))
                return dir.FullName;

            if (File.Exists(Path.Combine(dir.FullName, "src", "MimesisPlayerEnhancement.sln")))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    public static string RequireAssemblyCSharp(string managedPath)
    {
        string assemblyPath = Path.Combine(managedPath, "Assembly-CSharp.dll");
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException(
                $"Assembly-CSharp.dll not found in {managedPath}. Run ./scripts/bootstrap-deps.sh or point --managed at MIMESIS_Data/Managed.");
        }

        return assemblyPath;
    }
}
