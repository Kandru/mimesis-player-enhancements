using System.Collections.Concurrent;
using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    internal static class ReflectionFieldCache
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly ConcurrentDictionary<(Type Type, string Name), FieldInfo?> Fields = new();

        internal static FieldInfo? GetField(Type type, string name)
        {
            return Fields.GetOrAdd((type, name), static key => key.Type.GetField(key.Name, InstanceFlags));
        }

        internal static FieldInfo? GetField(object target, string name)
        {
            return GetField(target.GetType(), name);
        }
    }
}
