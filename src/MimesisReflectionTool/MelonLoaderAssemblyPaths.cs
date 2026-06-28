namespace MimesisReflectionTool;

internal static class MelonLoaderAssemblyPaths
{
    public static string Resolve(string? melonLoaderPath, string? gamePath)
    {
        if (!string.IsNullOrWhiteSpace(melonLoaderPath))
            return Path.GetFullPath(melonLoaderPath);

        if (!string.IsNullOrWhiteSpace(gamePath))
        {
            string fromGame = Path.Combine(gamePath, "MelonLoader", "net35");
            if (Directory.Exists(fromGame))
                return Path.GetFullPath(fromGame);
        }

        string? envGame = Environment.GetEnvironmentVariable("MIMESIS_PATH");
        if (!string.IsNullOrWhiteSpace(envGame))
        {
            string fromEnv = Path.Combine(envGame, "MelonLoader", "net35");
            if (Directory.Exists(fromEnv))
                return Path.GetFullPath(fromEnv);
        }

        string repoRoot = FindRepoRoot();
        string bootstrap = Path.Combine(repoRoot, "deps", "reference", "MelonLoader", "net35");
        if (Directory.Exists(bootstrap))
            return bootstrap;

        throw new InvalidOperationException(
            "Could not find MelonLoader assemblies. Pass --melonloader <path>, --game <MIMESIS root>, set MIMESIS_PATH, " +
            "or run ./scripts/bootstrap-deps.sh from the repo root.");
    }

    public static string RequireMelonLoader(string melonLoaderPath)
    {
        string assemblyPath = Path.Combine(melonLoaderPath, "MelonLoader.dll");
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException(
                $"MelonLoader.dll not found in {melonLoaderPath}. Run ./scripts/bootstrap-deps.sh or point --melonloader at MelonLoader/net35.");
        }

        return assemblyPath;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "deps", "reference", "MelonLoader", "net35")))
                return dir.FullName;

            if (File.Exists(Path.Combine(dir.FullName, "src", "MimesisPlayerEnhancement.sln")))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
