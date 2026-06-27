using System.Reflection;
using System.Runtime.Loader;

namespace MimesisInspectionTool;

internal sealed class MimesisMetadataContext : IDisposable
{
    private readonly MetadataLoadContext _context;
    private readonly Assembly _assemblyCSharp;

    public MimesisMetadataContext(string managedPath)
    {
        string assemblyPath = ManagedAssemblyPaths.RequireAssemblyCSharp(managedPath);
        var assemblyPaths = new List<string>(Directory.EnumerateFiles(managedPath, "*.dll"));

        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)
                              ?? throw new InvalidOperationException("Could not locate runtime assemblies.");
        foreach (string runtimeAssembly in Directory.EnumerateFiles(runtimeDir, "*.dll"))
        {
            if (!assemblyPaths.Contains(runtimeAssembly))
                assemblyPaths.Add(runtimeAssembly);
        }

        var resolver = new PathAssemblyResolver(assemblyPaths);
        _context = new MetadataLoadContext(resolver, typeof(object).Assembly.GetName().Name);
        _assemblyCSharp = _context.LoadFromAssemblyPath(assemblyPath);
    }

    public Assembly AssemblyCSharp => _assemblyCSharp;

    public Type? FindType(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        Type? direct = _assemblyCSharp.GetType(name, throwOnError: false, ignoreCase: false);
        if (direct != null)
            return direct;

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

    public Type RequireType(string name)
    {
        Type? type = FindType(name);
        if (type == null)
            throw new InvalidOperationException($"Type not found: {name}");

        return type;
    }

    public void Dispose() => _context.Dispose();
}
