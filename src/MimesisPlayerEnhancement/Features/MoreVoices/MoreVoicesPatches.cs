using System;
using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    public static class MoreVoicesPatches
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
                if (limits == null)
                {
                    return;
                }

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

                _wasApplying = true;
                return;
            }

            if (!_wasApplying)
            {
                return;
            }

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

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("OnStartClient/SpeechEventArchive", AccessTools.Method(typeof(SpeechEventArchive), "OnStartClient")),
                ("RemoveLowerValueEventsIfExceeded/SpeechEventArchive",
                    AccessTools.Method(typeof(SpeechEventArchive), "RemoveLowerValueEventsIfExceeded")),
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
    }
}
