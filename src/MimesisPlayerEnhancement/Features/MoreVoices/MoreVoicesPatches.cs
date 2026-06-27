using System;
using System.Reflection;
using HarmonyLib;
using Mimic.Voice.SpeechSystem;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MoreVoices;

public static class MoreVoicesPatches
{
    private const string Feature = "MoreVoices";

    private const BindingFlags InstanceFieldFlags =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly FieldInfo? MaxEventsField =
        typeof(SpeechEventArchive).GetField("maxEvents", InstanceFieldFlags);

    private static readonly FieldInfo? MaxDeathMatchEventsField =
        typeof(SpeechEventArchive).GetField("maxDeathMatchEvents", InstanceFieldFlags);

    private static readonly FieldInfo? MaxOutDoorEventsField =
        typeof(SpeechEventArchive).GetField("maxOutDoorEvents", InstanceFieldFlags);

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        harmony.CreateClassProcessor(typeof(MoreVoicesPatches)).Patch();
        ModLog.Info(Feature, "Patches applied.");
    }

    /// <summary>Updates voice limits on all live archives after config changes.</summary>
    public static void RefreshFromConfig()
    {
        if (!ModConfig.EnableMoreVoices.Value)
            return;

        int max = ModConfig.MaxVoiceEvents.Value;
        int updated = 0;

        foreach (var archive in UnityEngine.Object.FindObjectsByType<SpeechEventArchive>(FindObjectsSortMode.None))
        {
            if (archive == null)
                continue;

            if (ApplyLimitsToArchive(archive, max))
                updated++;
        }

        if (updated > 0)
        {
            ModLog.Info(Feature, $"Refreshed voice limits on {updated} archive(s) — maxCap={max}.");
        }
        else
        {
            ModLog.Debug(Feature, $"Voice limit refresh complete — maxCap={max}, no active archives.");
        }
    }

    [HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
    public static class SpeechEventArchiveLimitsPatch
    {
        [HarmonyPrefix]
        public static void Prefix(SpeechEventArchive __instance)
        {
            if (!ModConfig.EnableMoreVoices.Value || __instance == null)
                return;

            ApplyLimitsToArchive(__instance, ModConfig.MaxVoiceEvents.Value);
        }

        [HarmonyPostfix]
        public static void Postfix(SpeechEventArchive __instance)
        {
            if (!ModConfig.EnableMoreVoices.Value || __instance == null)
                return;

            ModLog.Debug(
                Feature,
                $"Voice archive started — maxCap={ModConfig.MaxVoiceEvents.Value}, " +
                $"maxEvents={GetFieldValue(__instance, MaxEventsField)}, " +
                $"{VoiceEventStats.DescribePlayer(__instance)}");
        }
    }

    private static bool ApplyLimitsToArchive(SpeechEventArchive archive, int max)
    {
        try
        {
            int oldMaxEvents = GetFieldValue(archive, MaxEventsField);
            int oldMaxDeathMatch = GetFieldValue(archive, MaxDeathMatchEventsField);
            int oldMaxOutdoor = GetFieldValue(archive, MaxOutDoorEventsField);

            SetFieldValue(archive, MaxEventsField, max);
            SetFieldValue(archive, MaxDeathMatchEventsField, max);
            SetFieldValue(archive, MaxOutDoorEventsField, max);

            ModLog.Debug(
                Feature,
                $"Applied instance limits -> {max} " +
                $"(was maxEvents={oldMaxEvents}, maxDeathMatch={oldMaxDeathMatch}, maxOutdoor={oldMaxOutdoor}).");
            return true;
        }
        catch (Exception ex)
        {
            ModLog.Warn(Feature, $"Failed to set voice limit fields: {ex.Message}");
            return false;
        }
    }

    private static int GetFieldValue(SpeechEventArchive archive, FieldInfo? field)
    {
        if (field == null)
            throw new MissingFieldException(typeof(SpeechEventArchive).FullName, "voice limit field");

        return (int)field.GetValue(archive)!;
    }

    private static void SetFieldValue(SpeechEventArchive archive, FieldInfo? field, int value)
    {
        if (field == null)
            throw new MissingFieldException(typeof(SpeechEventArchive).FullName, "voice limit field");

        field.SetValue(archive, value);
    }
}
