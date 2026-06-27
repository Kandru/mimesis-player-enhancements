using System;
using HarmonyLib;
using Mimic.Voice.SpeechSystem;
using MimesisPlayerEnhancement.Features.Persistence;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches;

[HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
public static class StatisticsSpeechEventArchivePatches
{
    [HarmonyPostfix]
    public static void Postfix(SpeechEventArchive __instance)
    {
        if (!ModConfig.EnableStatistics.Value)
            return;

        try
        {
            if (!MimesisSaveManager.IsHost())
                return;

            int slotId = MimesisSaveManager.GetCurrentSaveSlotId();
            if (!MMSaveGameData.CheckSaveSlotID(slotId, true))
                return;

            StatisticsTracker.RegisterConnectedPlayers();
        }
        catch (Exception ex)
        {
            ModLog.Warn("Statistics", $"SpeechEventArchive register failed: {ex.Message}");
        }
    }
}
