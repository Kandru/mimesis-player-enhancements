using System.Reflection;

namespace MimesisPlayerEnhancement.Tests.Infrastructure
{
    internal sealed class MimesisMetadataContext : IDisposable
    {
        private readonly MetadataLoadContext _context;
        private readonly Assembly _assemblyCSharp;

        internal MimesisMetadataContext(string managedPath)
        {
            string assemblyPath = ManagedAssemblyPaths.RequireAssemblyCSharp(managedPath);
            List<string> assemblyPaths = [];
            HashSet<string> seenNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (string path in Directory.EnumerateFiles(managedPath, "*.dll"))
            {
                string name = Path.GetFileName(path);
                if (seenNames.Add(name))
                {
                    assemblyPaths.Add(path);
                }
            }

            string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)
                                  ?? throw new InvalidOperationException("Could not locate runtime assemblies.");
            foreach (string runtimeAssembly in Directory.EnumerateFiles(runtimeDir, "*.dll"))
            {
                string name = Path.GetFileName(runtimeAssembly);
                if (seenNames.Add(name))
                {
                    assemblyPaths.Add(runtimeAssembly);
                }
            }

            var resolver = new PathAssemblyResolver(assemblyPaths);
            _context = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().Name);
            _assemblyCSharp = _context.LoadFromAssemblyPath(assemblyPath);
        }

        internal Assembly AssemblyCSharp => _assemblyCSharp;

        internal Type? FindType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            Type? direct = _assemblyCSharp.GetType(name, throwOnError: false, ignoreCase: false);
            if (direct != null)
            {
                return direct;
            }

            foreach (Type type in _assemblyCSharp.GetTypes())
            {
                if (string.Equals(type.Name, name, StringComparison.Ordinal)
                    || string.Equals(type.FullName, name, StringComparison.Ordinal))
                {
                    return type;
                }
            }

            return null;
        }

        internal Type RequireType(string name)
        {
            return FindType(name)
                ?? throw new InvalidOperationException($"Type not found: {name}");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
