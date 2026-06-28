using System.Reflection;

namespace MimesisReflectionTool;

internal sealed class MelonLoaderReflectionContext
{
    private readonly string _melonLoaderPath;
    private readonly Dictionary<string, Assembly> _loaded = new(StringComparer.OrdinalIgnoreCase);

    public MelonLoaderReflectionContext(string melonLoaderPath)
    {
        _melonLoaderPath = melonLoaderPath;
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        MelonLoader = LoadAssembly(MelonLoaderAssemblyPaths.RequireMelonLoader(melonLoaderPath));
    }

    public Assembly MelonLoader { get; }

    public Assembly LoadAssembly(string assemblyPath)
    {
        string fullPath = Path.GetFullPath(assemblyPath);
        if (_loaded.TryGetValue(fullPath, out Assembly? cached))
            return cached;

        Assembly assembly = Assembly.LoadFrom(fullPath);
        _loaded[fullPath] = assembly;
        return assembly;
    }

    public Assembly LoadAssemblyByName(string name)
    {
        string fileName = name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? name : name + ".dll";
        string assemblyPath = Path.Combine(_melonLoaderPath, fileName);
        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException($"Assembly not found in {_melonLoaderPath}: {fileName}");

        return LoadAssembly(assemblyPath);
    }

    public Type? FindType(Assembly assembly, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        Type? direct = assembly.GetType(name, throwOnError: false, ignoreCase: false);
        if (direct != null)
            return direct;

        foreach (Type type in SafeGetTypes(assembly))
        {
            if (string.Equals(type.Name, name, StringComparison.Ordinal)
                || string.Equals(type.FullName, name, StringComparison.Ordinal))
            {
                return type;
            }
        }

        return null;
    }

    public Type RequireType(Assembly assembly, string name)
    {
        Type? type = FindType(assembly, name);
        if (type == null)
            throw new InvalidOperationException($"Type not found in {assembly.GetName().Name}: {name}");

        return type;
    }

    private Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        string simpleName = new AssemblyName(args.Name).Name + ".dll";
        string path = Path.Combine(_melonLoaderPath, simpleName);
        return File.Exists(path) ? LoadAssembly(path) : null;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }
}
