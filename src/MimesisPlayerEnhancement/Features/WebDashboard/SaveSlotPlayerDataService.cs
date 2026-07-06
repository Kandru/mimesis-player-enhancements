using System;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class SaveSlotPlayerDataService
    {
        private const string Feature = "WebDashboard";

        private static string L(string key) => ModL10n.Get($"api.{key}");

        internal static WebDashboardActionResult RemovePlayer(int slotId, ulong steamId)
        {
            if (!WebDashboardGameState.IsHost())
            {
                return Fail(L("host_only"));
            }

            if (steamId == 0)
            {
                return Fail(L("invalid_steam_id"));
            }

            if (LocalPlayerHelper.IsLocalSteamId(steamId))
            {
                return Fail(L("cannot_delete_host"));
            }

            if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return Fail(L("no_active_save_slot"));
            }

            foreach (ulong connectedSteamId in StatisticsTracker.GetConnectedSteamIds())
            {
                if (connectedSteamId == steamId)
                {
                    return Fail(L("player_delete_connected"));
                }
            }

            bool hadStatistics = StatisticsTracker.TryGetPlayerDocument(steamId) != null;
            if (hadStatistics)
            {
                StatisticsTracker.RemovePlayer(steamId, waitForCompletion: true);
            }

            bool hadName = WebDashboardPlayerNameStore.TryGetName(slotId, steamId) != null;
            WebDashboardPlayerNameStore.ForgetName(slotId, steamId, waitForCompletion: true);

            int voiceRemoved = 0;
            if (ModConfig.EnablePersistence.Value)
            {
                voiceRemoved = SpeechEventPoolManager.RemovePlayer(steamId);
                try
                {
                    MimesisSaveManager.SaveMimesisData(slotId);
                    PersistenceWriteQueue.FlushAllSync();
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Speech sidecar rewrite after delete failed — {ex.Message}");
                }
            }

            if (!hadStatistics && !hadName && voiceRemoved == 0)
            {
                return Fail(L("player_delete_not_found"));
            }

            WebDashboardLeaderboardCache.Clear();
            WebDashboardSnapshotCache.MarkDirty();
            WebDashboardSnapshotCache.RequestFullPublish();

            ModLog.Info(Feature, $"Deleted player data — steamId={steamId}, slot={slotId}");
            return new WebDashboardActionResult
            {
                Success = true,
                Message = L("player_delete_success"),
            };
        }

        private static WebDashboardActionResult Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
