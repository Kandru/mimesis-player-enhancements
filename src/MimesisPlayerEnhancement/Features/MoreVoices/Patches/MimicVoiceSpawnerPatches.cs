namespace MimesisPlayerEnhancement.Features.MoreVoices.Patches
{
    [HarmonyPatch(typeof(MimicVoiceSpawner), "GetAllDissonancePlayers")]
    internal static class GetAllDissonancePlayersPatch
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref Dictionary<string, FishNetDissonancePlayer> __result)
        {
            if (!VoicePerformanceRuntime.IsActive)
            {
                return true;
            }

            __result = VoiceDissonancePlayerCache.GetPlayers();
            return false;
        }
    }

    [HarmonyPatch(typeof(MimicVoiceSpawner), "GetAllMimicActors")]
    internal static class GetAllMimicActorsPatch
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref Dictionary<int, ProtoActor> __result)
        {
            if (!VoicePerformanceRuntime.IsActive)
            {
                return true;
            }

            __result = VoiceMimicActorCache.GetMimicActors();
            return false;
        }
    }
}
