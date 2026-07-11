using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class VoicePerformancePatches
    {
        private const string Feature = "MoreVoices";

        private static readonly FieldInfo? EventsField =
            AccessTools.Field(typeof(SpeechEventArchive), "events");

        private static readonly MethodInfo? CreateAudioClipMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "CreateAudioClip");

        private static readonly MethodInfo? ObserverRpcPlayOnActorLogicMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "RpcLogic___ObserverRpcPlayOnActor_1543699021");

        private static readonly MethodInfo? ObserverRpcPlayOnNonMimicLogicMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "RpcLogic___ObserverRpcPlayOnNonMimicMonster_1543699021");

        private static readonly MethodInfo TryGetSpeechEventByIdMethod =
            AccessTools.Method(typeof(VoiceWarmCache), nameof(VoiceWarmCache.TryGetSpeechEventById));

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(VoicePerformancePatches)));

            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        [HarmonyPatch(typeof(SpeechEventArchive), nameof(SpeechEventArchive.GetWarmedUpSpeechEvents))]
        internal static class GetWarmedUpSpeechEventsPatch
        {
            [HarmonyPrefix]
            internal static bool Prefix(SpeechEventArchive __instance, ref List<SpeechEvent> __result)
            {
                if (!VoicePerformanceRuntime.IsActive)
                {
                    return true;
                }

                __result = VoiceWarmCache.GetWarmedEvents(__instance);
                return false;
            }
        }

        [HarmonyPatch(typeof(SpeechEventArchive), "get_WarmedUpCount")]
        internal static class WarmedUpCountPatch
        {
            [HarmonyPrefix]
            internal static bool Prefix(SpeechEventArchive __instance, ref int __result)
            {
                if (!VoicePerformanceRuntime.IsActive)
                {
                    return true;
                }

                __result = VoiceWarmCache.GetWarmedCount(__instance);
                return false;
            }
        }

        [HarmonyPatch(typeof(SpeechEventArchive), nameof(SpeechEventArchive.OnStartClient))]
        internal static class OnStartClientPatch
        {
            [HarmonyPostfix]
            internal static void Postfix(SpeechEventArchive __instance)
            {
                try
                {
                    VoiceWarmCache.Attach(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Voice cache attach failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(SpeechEventArchive), nameof(SpeechEventArchive.OnStopClient))]
        internal static class OnStopClientPatch
        {
            [HarmonyPrefix]
            internal static void Prefix(SpeechEventArchive __instance)
            {
                try
                {
                    VoiceWarmCache.Detach(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Voice cache detach failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(SpeechEventArchive), "CreateAudioClip")]
        internal static class CreateAudioClipPatch
        {
            [HarmonyPrefix]
            internal static bool Prefix(SpeechEvent speechEvent, ref object __result)
            {
                if (!VoicePerformanceRuntime.IsActive || speechEvent == null)
                {
                    return true;
                }

                if (VoiceClipCache.TryGet(speechEvent.Id, out UnityEngine.Object? cached) && cached != null)
                {
                    __result = cached;
                    return false;
                }

                return true;
            }

            [HarmonyPostfix]
            internal static void Postfix(SpeechEvent speechEvent, object __result)
            {
                if (!VoicePerformanceRuntime.IsActive || speechEvent == null || __result == null)
                {
                    return;
                }

                VoiceClipCache.Store(speechEvent.Id, (UnityEngine.Object)__result);
            }
        }

        [HarmonyPatch(typeof(SpeechEventArchive), "RpcLogic___ObserverRpcPlayOnActor_1543699021")]
        internal static class ObserverRpcPlayOnActorLookupPatch
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (EventsField == null)
                {
                    return instructions;
                }

                return VoicePerformanceIl.ReplaceSpeechEventFind(instructions, EventsField, TryGetSpeechEventByIdMethod);
            }
        }

        [HarmonyPatch(typeof(SpeechEventArchive), "RpcLogic___ObserverRpcPlayOnNonMimicMonster_1543699021")]
        internal static class ObserverRpcPlayOnNonMimicLookupPatch
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (EventsField == null)
                {
                    return instructions;
                }

                return VoicePerformanceIl.ReplaceSpeechEventFind(instructions, EventsField, TryGetSpeechEventByIdMethod);
            }
        }

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

        [HarmonyPatch(typeof(SpeechEventAdditionalGameData), nameof(SpeechEventAdditionalGameData.PickBestMatch))]
        internal static class PickBestMatchPatch
        {
            [HarmonyPrefix]
            internal static bool Prefix(
                MimicVoiceSpawner.MimicContext context,
                List<(string playerID, SpeechEvent evt)> allEvents,
                SpeechEventAdditionalGameData curGameData,
                bool periodic,
                int pickCount,
                float playTimeIntervalRandom,
                out SpeechEvent? speechEvent,
                out string mimickingPlayerID,
                out string pickReason,
                ref bool __result)
            {
                if (!VoicePerformanceRuntime.IsActive)
                {
                    speechEvent = null;
                    mimickingPlayerID = string.Empty;
                    pickReason = string.Empty;
                    return true;
                }

                __result = VoicePickBestMatch.TryPick(
                    context,
                    allEvents,
                    curGameData,
                    periodic,
                    pickCount,
                    playTimeIntervalRandom,
                    out speechEvent,
                    out mimickingPlayerID,
                    out pickReason);
                return false;
            }
        }

        internal static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("GetWarmedUpSpeechEvents/SpeechEventArchive",
                    AccessTools.Method(typeof(SpeechEventArchive), nameof(SpeechEventArchive.GetWarmedUpSpeechEvents))),
                ("WarmedUpCount/SpeechEventArchive",
                    AccessTools.PropertyGetter(typeof(SpeechEventArchive), nameof(SpeechEventArchive.WarmedUpCount))),
                ("OnStartClient/SpeechEventArchive (voice cache)",
                    AccessTools.Method(typeof(SpeechEventArchive), nameof(SpeechEventArchive.OnStartClient))),
                ("OnStopClient/SpeechEventArchive (voice cache)",
                    AccessTools.Method(typeof(SpeechEventArchive), nameof(SpeechEventArchive.OnStopClient))),
                ("CreateAudioClip/SpeechEventArchive", CreateAudioClipMethod),
                ("RpcLogic___ObserverRpcPlayOnActor/SpeechEventArchive", ObserverRpcPlayOnActorLogicMethod),
                ("RpcLogic___ObserverRpcPlayOnNonMimicMonster/SpeechEventArchive", ObserverRpcPlayOnNonMimicLogicMethod),
                ("GetAllDissonancePlayers/MimicVoiceSpawner",
                    AccessTools.Method(typeof(MimicVoiceSpawner), "GetAllDissonancePlayers")),
                ("GetAllMimicActors/MimicVoiceSpawner",
                    AccessTools.Method(typeof(MimicVoiceSpawner), "GetAllMimicActors")),
                ("PickBestMatch/SpeechEventAdditionalGameData",
                    AccessTools.Method(typeof(SpeechEventAdditionalGameData), nameof(SpeechEventAdditionalGameData.PickBestMatch))),
            ]);
        }
    }
}
