using System;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class VActorDictCapacityPatches
    {
        private const string Feature = "MorePlayers";

        [HarmonyPatch(typeof(IVroom), MethodType.Constructor, [typeof(VRoomManager), typeof(long), typeof(IVRoomProperty), typeof(OnCreateRoomDelegate)])]
        internal static class IVroomConstructorPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(IVroom __instance)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return;
                }

                try
                {
                    VActorDictCapacity.ApplyToRoom(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Room _vPlayerDict cap patch failed — {ex.Message}");
                }
            }
        }
    }
}
