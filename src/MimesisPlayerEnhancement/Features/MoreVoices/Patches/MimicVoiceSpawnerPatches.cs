namespace MimesisPlayerEnhancement.Features.MoreVoices.Patches
{
    // game@0.3.1 Assembly-CSharp/Mimic.Voice/MimicVoiceSpawner.cs:L296-314
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

    // game@0.3.1 Assembly-CSharp/Mimic.Voice/MimicVoiceSpawner.cs:L284-294
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
