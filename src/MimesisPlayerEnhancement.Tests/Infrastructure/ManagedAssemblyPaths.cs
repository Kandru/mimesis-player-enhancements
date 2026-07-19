namespace MimesisPlayerEnhancement.Tests.Infrastructure
{
    internal static class ManagedAssemblyPaths
    {
        internal static string Resolve(string? managedPath = null, string? gamePath = null)
        {
            if (!string.IsNullOrWhiteSpace(managedPath))
            {
                return Path.GetFullPath(managedPath);
            }

            if (!string.IsNullOrWhiteSpace(gamePath))
            {
                string fromGame = Path.Combine(gamePath, "MIMESIS_Data", "Managed");
                if (Directory.Exists(fromGame))
                {
                    return Path.GetFullPath(fromGame);
                }
            }

            string? envGame = Environment.GetEnvironmentVariable("MIMESIS_PATH");
            if (!string.IsNullOrWhiteSpace(envGame))
            {
                string fromEnv = Path.Combine(envGame, "MIMESIS_Data", "Managed");
                if (Directory.Exists(fromEnv))
                {
                    return Path.GetFullPath(fromEnv);
                }
            }

            string repoRoot = FindRepoRoot();
            string bootstrap = Path.Combine(repoRoot, "deps", "reference", "Managed");
            return Directory.Exists(bootstrap)
                ? bootstrap
                : throw new InvalidOperationException(
                    "Could not find game assemblies. Set MIMESIS_PATH, pass a managed path, " +
                    "or run make deps from the repo root.");
        }

        internal static string RequireAssemblyCSharp(string managedPath)
        {
            string assemblyPath = Path.Combine(managedPath, "Assembly-CSharp.dll");
            return !File.Exists(assemblyPath)
                ? throw new FileNotFoundException(
                    $"Assembly-CSharp.dll not found in {managedPath}. Run make deps.")
                : assemblyPath;
        }

        private static string FindRepoRoot()
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "deps", "reference", "Managed")))
                {
                    return dir.FullName;
                }

                if (File.Exists(Path.Combine(dir.FullName, "src", "MimesisPlayerEnhancement.sln")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("Could not locate repository root.");
        }
    }
}
