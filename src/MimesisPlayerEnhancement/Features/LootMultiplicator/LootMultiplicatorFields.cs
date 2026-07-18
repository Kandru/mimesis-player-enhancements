using System.Reflection;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootMultiplicatorFields
    {
        internal const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static readonly FieldInfo SpawnedActorDatasField =
            Field(typeof(DungeonRoom), "_spawnedActorDatas");

        internal static FieldInfo Field(Type type, string name)
        {
            FieldInfo? field = type.GetField(name, InstanceFlags) ?? AccessTools.Field(type, name);
            return field ?? throw new InvalidOperationException($"{type.Name}.{name} not found");
        }
    }
}
