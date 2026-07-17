namespace MimesisPlayerEnhancement.Features.MimicTuning.Patches
{
    [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.SetPossession))]
    internal static class SetPossessionStopVoicePostfix
    {
        private const string Feature = "MimicTuning";

        [HarmonyPostfix]
        internal static void Postfix(ProtoActor __instance, ProtoActor.PossessionState inState)
        {
            if (inState != ProtoActor.PossessionState.Possessed)
            {
                return;
            }

            try
            {
                __instance.StopVoiceOnActor();
                if (ModConfig.EnableDebugLogging.Value)
                {
                    ModLog.Debug(Feature, $"Stopped archived voice on possession — mimic={__instance.ActorID}");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SetPossession stop-voice postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.PlayVoiceOnActor))]
    internal static class PlayVoiceOnActorPossessedPrefix
    {
        private const string Feature = "MimicTuning";

        [HarmonyPrefix]
        internal static bool Prefix(ProtoActor __instance, ref bool __result)
        {
            if (__instance.Possession.State != ProtoActor.PossessionState.Possessed)
            {
                return true;
            }

            try
            {
                __result = false;
                if (ModConfig.EnableDebugLogging.Value)
                {
                    ModLog.Debug(Feature, $"Blocked archived voice on possessed mimic — mimic={__instance.ActorID}");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"PlayVoiceOnActor possessed prefix failed — {ex.Message}");
            }

            return false;
        }
    }
}
