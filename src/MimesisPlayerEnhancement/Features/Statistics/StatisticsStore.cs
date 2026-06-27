using System;
using System.Collections.Generic;
using System.IO;
using MimesisPlayerEnhancement.Features.Persistence;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.Statistics;

public static class StatisticsStore
{
    private const string Feature = "Statistics";
    private const string StatisticsFolder = "statistics";
    private const string PlayersFolder = "players";
    private const string LeaderboardFile = "leaderboard.json";
    private const string BackupSuffix = ".bak";
    private const string TempSuffix = ".tmp";
    private const int MaxRecentSessions = 20;

    public static int MaxRecentSessionsPerPlayer => MaxRecentSessions;

    public static string? GetStatisticsRootPath(int slotId)
    {
        string? slotPath = MimesisSaveManager.GetMimesisSlotPath(slotId);
        if (string.IsNullOrEmpty(slotPath)) return null;
        return Path.Combine(slotPath, StatisticsFolder);
    }

    public static string? GetPlayerFilePath(int slotId, ulong steamId)
    {
        string? root = GetStatisticsRootPath(slotId);
        if (string.IsNullOrEmpty(root)) return null;
        return Path.Combine(root, PlayersFolder, steamId + ".json");
    }

    public static string? GetLeaderboardFilePath(int slotId)
    {
        string? root = GetStatisticsRootPath(slotId);
        if (string.IsNullOrEmpty(root)) return null;
        return Path.Combine(root, LeaderboardFile);
    }

    public static bool IsActiveHostWithSlot()
    {
        if (!MimesisSaveManager.IsHost()) return false;
        int slotId = MimesisSaveManager.GetCurrentSaveSlotId();
        return MMSaveGameData.CheckSaveSlotID(slotId, true);
    }

    public static int GetActiveSlotId() => MimesisSaveManager.GetCurrentSaveSlotId();

    public static void SafeWriteText(string filePath, string text)
    {
        string tmpPath = filePath + TempSuffix;
        string bakPath = filePath + BackupSuffix;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(tmpPath, text);
        if (File.Exists(filePath))
        {
            try { File.Copy(filePath, bakPath, true); }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Backup failed for {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }
        if (File.Exists(filePath))
            File.Delete(filePath);
        File.Move(tmpPath, filePath);
    }

    public static string? SafeReadText(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                string text = File.ReadAllText(filePath);
                if (!string.IsNullOrEmpty(text)) return text;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Read failed ({Path.GetFileName(filePath)}): {ex.Message}");
            }
        }

        string bakPath = filePath + BackupSuffix;
        if (File.Exists(bakPath))
        {
            try
            {
                string text = File.ReadAllText(bakPath);
                if (!string.IsNullOrEmpty(text))
                {
                    ModLog.Warn(Feature, $"Recovered from backup: {Path.GetFileName(bakPath)}");
                    return text;
                }
            }
            catch (Exception ex)
            {
                ModLog.Error(Feature, $"Backup read failed: {ex.Message}");
            }
        }

        return null;
    }

    public static PlayerStatisticsDocument LoadPlayer(int slotId, ulong steamId)
    {
        string? path = GetPlayerFilePath(slotId, steamId);
        if (string.IsNullOrEmpty(path))
            return NewPlayerDocument(steamId);

        string? json = SafeReadText(path);
        if (string.IsNullOrEmpty(json))
            return NewPlayerDocument(steamId);

        var doc = StatisticsJson.DeserializePlayer(json);
        if (doc == null)
        {
            ModLog.Warn(Feature, $"Corrupt player statistics — replacing with fresh document: {Path.GetFileName(path)}");
            return NewPlayerDocument(steamId);
        }

        doc.SteamId = steamId;
        doc.RecentSessions ??= new List<SessionStats>();
        doc.Global ??= new GlobalStats();
        return doc;
    }

    public static void SavePlayer(int slotId, PlayerStatisticsDocument doc)
    {
        string? path = GetPlayerFilePath(slotId, doc.SteamId);
        if (string.IsNullOrEmpty(path)) return;

        TrimRecentSessions(doc);
        doc.Version = PlayerStatisticsDocument.CurrentVersion;
        SafeWriteText(path, StatisticsJson.SerializePlayer(doc));
    }

    public static void SaveLeaderboard(int slotId, LeaderboardDocument leaderboard)
    {
        string? path = GetLeaderboardFilePath(slotId);
        if (string.IsNullOrEmpty(path)) return;

        leaderboard.Version = LeaderboardDocument.CurrentVersion;
        leaderboard.SaveSlotId = slotId;
        leaderboard.UpdatedAtUtc = DateTime.UtcNow;
        SafeWriteText(path, StatisticsJson.SerializeLeaderboard(leaderboard));
    }


    public static void LoadAllPlayersForSlot(int slotId, Dictionary<ulong, PlayerStatisticsDocument> cache)
    {
        string? root = GetStatisticsRootPath(slotId);
        if (string.IsNullOrEmpty(root)) return;

        string playersDir = Path.Combine(root, PlayersFolder);
        if (!Directory.Exists(playersDir)) return;

        foreach (string file in Directory.GetFiles(playersDir, "*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (!ulong.TryParse(name, out ulong steamId)) continue;
            cache[steamId] = LoadPlayer(slotId, steamId);
        }
    }

    public static void DeleteStatisticsData(int slotId)
    {
        string? root = GetStatisticsRootPath(slotId);
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;
        try
        {
            Directory.Delete(root, true);
            ModLog.Info(Feature, $"Deleted statistics data for slot {slotId}.");
        }
        catch (Exception ex)
        {
            ModLog.Warn(Feature, $"DeleteStatisticsData: {ex.Message}");
        }
    }

    private static PlayerStatisticsDocument NewPlayerDocument(ulong steamId) => new()
    {
        SteamId = steamId,
        Global = new GlobalStats(),
        RecentSessions = new List<SessionStats>(),
    };

    private static void TrimRecentSessions(PlayerStatisticsDocument doc)
    {
        while (doc.RecentSessions.Count > MaxRecentSessions)
            doc.RecentSessions.RemoveAt(0);
    }
}
