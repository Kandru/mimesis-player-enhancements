using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.SpawnScaling.Patches
{
    internal static class SpawnScalingPatchHelpers
    {
        internal const string Feature = "SpawnScaling";

        internal const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static readonly Type[] SpawnMonsterParameterTypes =
        [
            typeof(int),
            typeof(SpawnedActorData),
            typeof(bool),
            typeof(string),
            typeof(string),
            typeof(ReasonOfSpawn),
        ];

        internal static MethodBase? ResolveSpawnMonsterMethod()
        {
            return AccessTools.Method(typeof(IVroom), "SpawnMonster", SpawnMonsterParameterTypes);
        }
    }
}
