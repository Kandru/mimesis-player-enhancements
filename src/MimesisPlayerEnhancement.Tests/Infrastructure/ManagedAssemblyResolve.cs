using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace MimesisPlayerEnhancement.Tests.Infrastructure
{
    /// <summary>
    /// Resolves game Managed DLLs at runtime for live reflection tests.
    /// Compile references use Private=false, so Unity/Steamworks/etc. are not copied next to the test host.
    /// MelonLoader is intentionally skipped — loading it hangs vstest.
    /// </summary>
    internal static class ManagedAssemblyResolve
    {
        private static readonly object Gate = new();
        private static bool _registered;
        private static string? _managedPath;

        [ModuleInitializer]
        internal static void Register()
        {
            lock (Gate)
            {
                if (_registered)
                {
                    return;
                }

                _registered = true;
                AssemblyLoadContext.Default.Resolving += OnResolving;
            }
        }

        private static Assembly? OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            string? simpleName = name.Name;
            if (string.IsNullOrWhiteSpace(simpleName))
            {
                return null;
            }

            // MelonLoader hangs the test host when loaded into vstest.
            if (string.Equals(simpleName, "MelonLoader", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string managedPath = _managedPath ??= ManagedAssemblyPaths.Resolve();
            string path = Path.Combine(managedPath, simpleName + ".dll");
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return context.LoadFromAssemblyPath(path);
            }
            catch (FileLoadException)
            {
                // Already loaded under a different identity/path.
                return null;
            }
        }
    }
}
