using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    public static class MoreVoicesPatches
    {
        private const string Feature = "MoreVoices";
        private static bool _wasApplying;

        private static readonly MethodInfo? BroadcastNewEventWithRemovalMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "ServerRpcBroadcastNewEventWithRemoval");

        [ThreadStatic]
        private static List<long>? _lastRemovalIds;

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            if (!SpeechEventArchiveLimits.FieldsAvailable)
            {
                ModLog.Warn(Feature, "SpeechEventArchive limit fields not found — voice cap patches may not apply");
            }

            if (AccessTools.Method(typeof(SpeechEventArchive), "RemoveLowerValueEventsIfExceeded") == null)
            {
                ModLog.Warn(Feature, "RemoveLowerValueEventsIfExceeded not found — re-trim may not apply");
            }

            if (BroadcastNewEventWithRemovalMethod == null)
            {
                ModLog.Warn(Feature, "ServerRpcBroadcastNewEventWithRemoval not found — hub sync may not apply");
            }

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(MoreVoicesPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        /// <summary>Updates voice limits on all live archives after config changes.</summary>
        public static void RefreshFromConfig()
        {
            if (ModConfig.EnableMoreVoices.Value)
            {
                SpeechEventArchiveLimits.PoolLimits? limits = SpeechEventArchiveLimits.ResolveFromConfig();
                if (limits != null)
                {
                    int updated = 0;
                    foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
                    {
                        if (archive == null)
                        {
                            continue;
                        }

                        if (SpeechEventArchiveLimits.TryApply(archive, retrimOnDecrease: true))
                        {
                            updated++;
                        }
                    }

                    string caps = SpeechEventArchiveLimits.FormatEffectiveCaps(
                        SpeechEventArchiveLimits.ToEffectiveCaps(limits.Value));
                    if (updated > 0)
                    {
                        ModLog.Info(Feature, $"Refreshed voice limits on {updated} archive(s) — {caps}.");
                    }
                    else
                    {
                        ModLog.Debug(Feature, $"Voice limit refresh complete — {caps}, no active archives.");
                    }
                }

                _wasApplying = true;
            }
            else if (_wasApplying)
            {
                int restored = 0;
                foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
                {
                    if (archive == null)
                    {
                        continue;
                    }

                    if (SpeechEventArchiveLimits.TryRestoreVanilla(archive, retrimOnDecrease: true))
                    {
                        restored++;
                    }
                }

                _wasApplying = false;
                if (restored > 0)
                {
                    var vanillaCaps = SpeechEventArchiveLimits.ToEffectiveCaps(new SpeechEventArchiveLimits.PoolLimits(
                        SpeechEventArchiveLimits.VanillaMaxEvents,
                        SpeechEventArchiveLimits.VanillaMaxDeathMatchEvents,
                        SpeechEventArchiveLimits.VanillaMaxOutDoorEvents));
                    ModLog.Info(Feature, $"Restored vanilla voice limits on {restored} archive(s) — " +
                        SpeechEventArchiveLimits.FormatEffectiveCaps(vanillaCaps));
                }
                else
                {
                    ModLog.Debug(Feature, "Voice limit disable complete — no active archives to restore.");
                }
            }

            MoreVoicesRecording.ApplyRecordingState();
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("OnStartClient/SpeechEventArchive", AccessTools.Method(typeof(SpeechEventArchive), "OnStartClient")),
                ("RemoveLowerValueEventsIfExceeded/SpeechEventArchive",
                    AccessTools.Method(typeof(SpeechEventArchive), "RemoveLowerValueEventsIfExceeded")),
                ("SetVoiceMode/VoiceManager", AccessTools.Method(typeof(VoiceManager), nameof(VoiceManager.SetVoiceMode))),
                ("EndPossessionToMimic/VoiceManager", AccessTools.Method(typeof(VoiceManager), nameof(VoiceManager.EndPossessionToMimic))),
                ("AddEvent/SpeechEventArchive", AccessTools.Method(typeof(SpeechEventArchive), "AddEvent")),
                ("OnSpeechEventRecorded/SpeechEventArchive",
                    AccessTools.Method(typeof(SpeechEventArchive), "OnSpeechEventRecorded")),
            ]);
        }

        internal static PlayerLifecycleContribution? TryDescribeArchiveStarted(SpeechEventArchive archive)
        {
            if (!ModConfig.EnableMoreVoices.Value || archive == null)
            {
                return null;
            }

            try
            {
                SpeechEventArchiveLimits.EffectiveCaps caps = SpeechEventArchiveLimits.ReadEffectiveCaps(archive);
                return new PlayerLifecycleContribution(
                    Feature,
                    $"caps {SpeechEventArchiveLimits.FormatEffectiveCaps(caps)}");
            }
            catch
            {
                return null;
            }
        }

        [HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
        [HarmonyPriority(-100)]
        internal static class SpeechEventArchiveOnStartClientPatch
        {
            [HarmonyPrefix]
            public static void Prefix(SpeechEventArchive __instance)
            {
                if (!ModConfig.EnableMoreVoices.Value || __instance == null)
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
            [HarmonyPrefix]
            public static void Prefix(SpeechEventArchive __instance)
            {
                if (!ModConfig.EnableMoreVoices.Value || __instance == null)
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

        [HarmonyPatch(typeof(VoiceManager), nameof(VoiceManager.SetVoiceMode))]
        internal static class VoiceManagerSetVoiceModePatch
        {
            [HarmonyPostfix]
            public static void Postfix(VoiceManager __instance, VoiceMode voiceMode)
            {
                if (!MoreVoicesRecording.IsFeatureActive()
                    || voiceMode != VoiceMode.PreGame
                    || !MoreVoicesRecording.ShouldRecordInCurrentHubScene())
                {
                    return;
                }

                try
                {
                    MoreVoicesVoiceAccess.StartRecording(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Hub voice recording postfix failed: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(VoiceManager), nameof(VoiceManager.EndPossessionToMimic))]
        internal static class VoiceManagerEndPossessionToMimicPatch
        {
            [HarmonyPostfix]
            public static void Postfix(VoiceManager __instance)
            {
                if (!MoreVoicesRecording.ShouldResumeRecordingAfterPossession())
                {
                    return;
                }

                try
                {
                    MoreVoicesVoiceAccess.StartRecording(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Possession voice recording resume failed: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(SpeechEventArchive), "AddEvent")]
        internal static class SpeechEventArchiveAddEventPatch
        {
            [HarmonyPostfix]
            public static void Postfix(List<long> __result)
            {
                _lastRemovalIds = __result;
            }
        }

        [HarmonyPatch(typeof(SpeechEventArchive), "OnSpeechEventRecorded")]
        internal static class SpeechEventArchiveOnSpeechEventRecordedPatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                _lastRemovalIds = null;
            }

            [HarmonyPostfix]
            public static void Postfix(SpeechEventArchive __instance, SpeechEvent speechEvent, bool isForce)
            {
                if (!MoreVoicesRecording.IsFeatureActive()
                    || BroadcastNewEventWithRemovalMethod == null
                    || __instance == null
                    || speechEvent == null
                    || MoreVoicesRecording.VanillaWouldSyncRecordedEvent(isForce)
                    || !MoreVoicesRecording.ShouldSyncRecordedEvent(isForce))
                {
                    return;
                }

                try
                {
                    long[] idsToRemove = _lastRemovalIds?.ToArray() ?? [];
                    BroadcastNewEventWithRemovalMethod.Invoke(__instance, [speechEvent, idsToRemove]);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Hub voice sync postfix failed: {ex.Message}");
                }
            }
        }
    }
}
