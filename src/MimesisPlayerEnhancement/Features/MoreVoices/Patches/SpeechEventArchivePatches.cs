using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MoreVoices.Patches
{
    internal static class SpeechEventArchivePatchSupport
    {
        internal static readonly FieldInfo? EventsField =
            AccessTools.Field(typeof(SpeechEventArchive), "events");

        internal static readonly MethodInfo? CreateAudioClipMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "CreateAudioClip");

        internal static readonly MethodInfo? ObserverRpcPlayOnActorLogicMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "RpcLogic___ObserverRpcPlayOnActor_1543699021");

        internal static readonly MethodInfo? ObserverRpcPlayOnNonMimicLogicMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "RpcLogic___ObserverRpcPlayOnNonMimicMonster_1543699021");

        internal static readonly MethodInfo TryGetSpeechEventByIdMethod =
            AccessTools.Method(typeof(VoiceWarmCache), nameof(VoiceWarmCache.TryGetSpeechEventById));
    }

    [HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
    [HarmonyPriority(-100)]
    internal static class SpeechEventArchiveOnStartClientPatch
    {
        private const string Feature = "MoreVoices";

        [HarmonyPrefix]
        public static void Prefix(SpeechEventArchive __instance)
        {
            if (!ModConfig.EnableMoreVoices.Value
                || !HostApplyGate.ShouldApplyHostOnlyFeature()
                || __instance == null)
            {
                return;
            }

            try
            {
                if (!SpeechEventArchiveLimits.TryApply(__instance, retrimOnDecrease: false))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Voice archive prefix failed: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SpeechEventArchive), "RemoveLowerValueEventsIfExceeded")]
    internal static class RemoveLowerValueEventsIfExceededPrefix
    {
        private const string Feature = "MoreVoices";

        [HarmonyPrefix]
        public static void Prefix(SpeechEventArchive __instance)
        {
            if (!ModConfig.EnableMoreVoices.Value
                || !HostApplyGate.ShouldApplyHostOnlyFeature()
                || __instance == null)
            {
                return;
            }

            try
            {
                _ = SpeechEventArchiveLimits.TryApplyFields(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Voice limit prefix before eviction failed: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SpeechEventArchive), "RemoveLowerValueEventsIfExceeded")]
    [HarmonyPriority(500)]
    internal static class RemoveLowerValueEventsIfExceededUnifiedPatch
    {
        private const string Feature = "MoreVoices";

        [HarmonyPrefix]
        public static bool Prefix(SpeechEventArchive __instance, ref List<long> __result)
        {
            if (!MoreVoicesUnify.IsActive
                || !HostApplyGate.ShouldApplyHostOnlyFeature()
                || __instance == null)
            {
                return true;
            }

            try
            {
                __result = SpeechEventArchiveUnifiedEviction.TryEvict(__instance);
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Unified voice eviction failed: {ex.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(SpeechEventArchive), "AddEvent")]
    internal static class SpeechEventArchiveAddEventPatch
    {
        [HarmonyPostfix]
        public static void Postfix(List<long> __result)
        {
            if (!ModConfig.EnableMoreVoices.Value)
            {
                return;
            }

            MoreVoicesPatchHelpers._lastRemovalIds = __result;
        }
    }

    [HarmonyPatch(typeof(SpeechEventArchive), "OnSpeechEventRecorded")]
    internal static class SpeechEventArchiveOnSpeechEventRecordedPatch
    {
        private const string Feature = "MoreVoices";

        [HarmonyPrefix]
        public static void Prefix()
        {
            MoreVoicesPatchHelpers._lastRemovalIds = null;
        }

        [HarmonyPostfix]
        public static void Postfix(SpeechEventArchive __instance, SpeechEvent speechEvent, bool isForce)
        {
            if (!MoreVoicesRecording.IsFeatureActive()
                || MoreVoicesPatchHelpers.BroadcastNewEventWithRemovalMethod == null
                || __instance == null
                || speechEvent == null
                || MoreVoicesRecording.VanillaWouldSyncRecordedEvent(isForce)
                || !MoreVoicesRecording.ShouldSyncRecordedEvent(isForce))
            {
                return;
            }

            try
            {
                long[] idsToRemove = MoreVoicesPatchHelpers._lastRemovalIds is { Count: > 0 } removalIds
                    ? [.. removalIds]
                    : [];
                MoreVoicesPatchHelpers.BroadcastNewEventWithRemovalMethod.Invoke(__instance, [speechEvent, idsToRemove]);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Hub voice sync postfix failed: {ex.Message}");
            }
        }
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
    internal static class OnStartClientVoiceCachePatch
    {
        private const string Feature = "MoreVoices";

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
        private const string Feature = "MoreVoices";

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
            if (SpeechEventArchivePatchSupport.EventsField == null)
            {
                return instructions;
            }

            return VoicePerformanceIl.ReplaceSpeechEventFind(
                instructions,
                SpeechEventArchivePatchSupport.EventsField,
                SpeechEventArchivePatchSupport.TryGetSpeechEventByIdMethod);
        }
    }

    [HarmonyPatch(typeof(SpeechEventArchive), "RpcLogic___ObserverRpcPlayOnNonMimicMonster_1543699021")]
    internal static class ObserverRpcPlayOnNonMimicLookupPatch
    {
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (SpeechEventArchivePatchSupport.EventsField == null)
            {
                return instructions;
            }

            return VoicePerformanceIl.ReplaceSpeechEventFind(
                instructions,
                SpeechEventArchivePatchSupport.EventsField,
                SpeechEventArchivePatchSupport.TryGetSpeechEventByIdMethod);
        }
    }
}
