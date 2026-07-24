using MimesisPlayerEnhancement.Features.MoreVoices.Patches;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class MoreVoicesPatches
    {
        private const string Feature = "MoreVoices";
        private static bool _wasApplying;

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

            if (MoreVoicesPatchHelpers.BroadcastNewEventWithRemovalMethod == null)
            {
                ModLog.Warn(Feature, "ServerRpcBroadcastNewEventWithRemoval not found — hub sync may not apply");
            }

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(MoreVoicesPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        /// <summary>Updates voice limits on all live archives after config changes.</summary>
        public static void RefreshFromConfig()
        {
            VoicePerformanceRuntime.RefreshFromConfig();

            if (MoreVoicesRuntime.ShouldApply())
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
                RestoreVanillaLimitsOnActiveArchives();
                _wasApplying = false;
            }

            MoreVoicesRecording.ApplyRecordingState();
        }

        private static void RestoreVanillaLimitsOnActiveArchives()
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

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("OnStartClient/SpeechEventArchive", AccessTools.Method(typeof(SpeechEventArchive), "OnStartClient")),
                ("RemoveLowerValueEventsIfExceeded/SpeechEventArchive",
                    AccessTools.Method(typeof(SpeechEventArchive), "RemoveLowerValueEventsIfExceeded")),
                ("PickBestMatch/SpeechEventAdditionalGameData",
                    AccessTools.Method(typeof(SpeechEventAdditionalGameData), nameof(SpeechEventAdditionalGameData.PickBestMatch))),
                ("SetVoiceMode/VoiceManager", AccessTools.Method(typeof(VoiceManager), nameof(VoiceManager.SetVoiceMode))),
                ("EndPossessionToMimic/VoiceManager", AccessTools.Method(typeof(VoiceManager), nameof(VoiceManager.EndPossessionToMimic))),
                ("EnterWaitingRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))),
                ("HandleLevelLoadComplete/VPlayer", AccessTools.Method(typeof(VPlayer), nameof(VPlayer.HandleLevelLoadComplete))),
                ("AddEvent/SpeechEventArchive", AccessTools.Method(typeof(SpeechEventArchive), "AddEvent")),
                ("OnSpeechEventRecorded/SpeechEventArchive",
                    AccessTools.Method(typeof(SpeechEventArchive), "OnSpeechEventRecorded")),
                ("GetWarmedUpSpeechEvents/SpeechEventArchive",
                    AccessTools.Method(typeof(SpeechEventArchive), nameof(SpeechEventArchive.GetWarmedUpSpeechEvents))),
                ("WarmedUpCount/SpeechEventArchive",
                    AccessTools.PropertyGetter(typeof(SpeechEventArchive), nameof(SpeechEventArchive.WarmedUpCount))),
                ("OnStopClient/SpeechEventArchive (voice cache)",
                    AccessTools.Method(typeof(SpeechEventArchive), nameof(SpeechEventArchive.OnStopClient))),
                ("CreateAudioClip/SpeechEventArchive", SpeechEventArchivePatchSupport.CreateAudioClipMethod),
                ("RpcLogic___ObserverRpcPlayOnActor/SpeechEventArchive", SpeechEventArchivePatchSupport.ObserverRpcPlayOnActorLogicMethod),
                ("RpcLogic___ObserverRpcPlayOnNonMimicMonster/SpeechEventArchive", SpeechEventArchivePatchSupport.ObserverRpcPlayOnNonMimicLogicMethod),
                ("GetAllDissonancePlayers/MimicVoiceSpawner",
                    AccessTools.Method(typeof(MimicVoiceSpawner), "GetAllDissonancePlayers")),
                ("GetAllMimicActors/MimicVoiceSpawner",
                    AccessTools.Method(typeof(MimicVoiceSpawner), "GetAllMimicActors")),
            ]);
        }
    }
}
