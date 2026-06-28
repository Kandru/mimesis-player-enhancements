using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;

namespace MimesisPlayerEnhancement;

/// <summary>
/// MelonPreferences-backed configuration. Values are stored in
/// UserData/MimesisPlayerEnhancement.cfg (separate from the global MelonPreferences.cfg).
/// </summary>
public static class ModConfig
{
    private const string CategoryId = "MimesisPlayerEnhancement";

    /// <summary>Fired when any preference value changes (UI save, file reload, or programmatic update).</summary>
    public static event Action? Changed;

    public static bool IsInitialized { get; private set; }

    public static string FilePath { get; private set; } = "";

    public static MelonPreferences_Category Category { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableMorePlayers { get; private set; } = null!;
    public static MelonPreferences_Entry<int> MaxPlayers { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableMoreVoices { get; private set; } = null!;
    public static MelonPreferences_Entry<int> MaxVoiceEvents { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnablePersistence { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableStatistics { get; private set; } = null!;
    public static MelonPreferences_Entry<int> SessionReconnectGraceMinutes { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> ShowStatisticsToasts { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableJoinAnytime { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableSpawnScaling { get; private set; } = null!;
    public static MelonPreferences_Entry<float> MimicSpawnMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleMimicSpawnsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> BossSpawnMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleBossSpawnsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> JakoSpawnMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleJakoSpawnsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> SpecialSpawnMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleSpecialSpawnsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> TrapSpawnMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleTrapSpawnsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> FixedSpawnRespawnDelayMinSeconds { get; private set; } = null!;
    public static MelonPreferences_Entry<float> FixedSpawnRespawnDelayMaxSeconds { get; private set; } = null!;
    public static MelonPreferences_Entry<float> OtherSpawnMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleOtherSpawnsByPlayerCount { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableDebugLogging { get; private set; } = null!;

    public static void Initialize(MelonLogger.Instance logger)
    {
        Category = MelonPreferences.CreateCategory(CategoryId, "Mimesis Player Enhancement");
        FilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "MimesisPlayerEnhancement.cfg");
        Category.SetFilePath(FilePath);

        EnableMorePlayers = Category.CreateEntry(
            "EnableMorePlayers",
            true,
            "Enable More Players",
            "Raise the multiplayer player cap above 4.");

        MaxPlayers = Category.CreateEntry(
            "MaxPlayers",
            999,
            "Max Players",
            "Maximum players in a session including the host (1 = solo, 2 = host + 1 client, etc.).");

        EnableMoreVoices = Category.CreateEntry(
            "EnableMoreVoices",
            true,
            "Enable More Voices",
            "Raise per-player voice recording limits.");

        MaxVoiceEvents = Category.CreateEntry(
            "MaxVoiceEvents",
            3000,
            "Max Voice Events",
            "Maximum stored voice events per player (default game limit is much lower).");

        EnablePersistence = Category.CreateEntry(
            "EnablePersistence",
            true,
            "Enable Voice Persistence",
            "Save and restore mimic voice recordings across save/load.");

        EnableStatistics = Category.CreateEntry(
            "EnableStatistics",
            true,
            "Enable Player Statistics",
            "Track per-session and global player statistics per save slot.");

        SessionReconnectGraceMinutes = Category.CreateEntry(
            "SessionReconnectGraceMinutes",
            5,
            "Session Reconnect Grace (minutes)",
            "Reuse the previous session when a player reconnects within this many minutes.");

        ShowStatisticsToasts = Category.CreateEntry(
            "ShowStatisticsToasts",
            true,
            "Show Statistics Toasts",
            "Show mod stats toasts in plain English (session intro for you, global stats on join/leave). Does not replace the game's own connect messages.");

        EnableJoinAnytime = Category.CreateEntry(
            "EnableJoinAnytime",
            true,
            "Enable Join Anytime",
            "Allow players to join a session after it has already started.");

        EnableSpawnScaling = Category.CreateEntry(
            "EnableSpawnScaling",
            true,
            "Enable Spawn Scaling",
            "Scale dungeon monster spawn budgets by type. Host only.");

        MimicSpawnMultiplier = Category.CreateEntry(
            "MimicSpawnMultiplier",
            1f,
            "Mimic Spawn Multiplier",
            "Mimic spawn budget multiplier (1 = vanilla, 2 = double).");

        AutoScaleMimicSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleMimicSpawnsByPlayerCount",
            true,
            "Auto Scale Mimic Spawns By Player Count",
            "When enabled, multiply mimic spawn budgets by player count / 4 for sessions with more than 4 players (stacks with MimicSpawnMultiplier).");

        BossSpawnMultiplier = Category.CreateEntry(
            "BossSpawnMultiplier",
            1f,
            "Boss Spawn Multiplier",
            "Boss spawn budget multiplier (1 = vanilla, 2 = double).");

        AutoScaleBossSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleBossSpawnsByPlayerCount",
            true,
            "Auto Scale Boss Spawns By Player Count",
            "When enabled, multiply boss spawn budgets by player count / 4 for sessions with more than 4 players (stacks with BossSpawnMultiplier).");

        JakoSpawnMultiplier = Category.CreateEntry(
            "JakoSpawnMultiplier",
            1f,
            "Jako Spawn Multiplier",
            "Jako (normal monster) spawn budget multiplier (1 = vanilla, 2 = double).");

        AutoScaleJakoSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleJakoSpawnsByPlayerCount",
            true,
            "Auto Scale Jako Spawns By Player Count",
            "When enabled, multiply jako spawn budgets by player count / 4 for sessions with more than 4 players (stacks with JakoSpawnMultiplier).");

        SpecialSpawnMultiplier = Category.CreateEntry(
            "SpecialSpawnMultiplier",
            1f,
            "Special Spawn Multiplier",
            "Special monster spawn budget multiplier (1 = vanilla, 2 = double).");

        AutoScaleSpecialSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleSpecialSpawnsByPlayerCount",
            true,
            "Auto Scale Special Spawns By Player Count",
            "When enabled, multiply special spawn budgets by player count / 4 for sessions with more than 4 players (stacks with SpecialSpawnMultiplier).");

        TrapSpawnMultiplier = Category.CreateEntry(
            "TrapSpawnMultiplier",
            1f,
            "Trap Spawn Multiplier",
            "Trap spawn multiplier (1 = vanilla, 2 = double). Map-placed traps use unused markers first, then respawn at the same marker when gone.");

        AutoScaleTrapSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleTrapSpawnsByPlayerCount",
            true,
            "Auto Scale Trap Spawns By Player Count",
            "When enabled, multiply trap spawn counts by player count / 4 for sessions with more than 4 players (stacks with TrapSpawnMultiplier).");

        FixedSpawnRespawnDelayMinSeconds = Category.CreateEntry(
            "FixedSpawnRespawnDelayMinSeconds",
            5f,
            "Fixed Spawn Respawn Delay Min Seconds",
            "Minimum random delay before a map-placed monster or trap respawns at the same marker when no unused markers remain.");

        FixedSpawnRespawnDelayMaxSeconds = Category.CreateEntry(
            "FixedSpawnRespawnDelayMaxSeconds",
            30f,
            "Fixed Spawn Respawn Delay Max Seconds",
            "Maximum random delay before a map-placed monster or trap respawns at the same marker when no unused markers remain.");

        OtherSpawnMultiplier = Category.CreateEntry(
            "OtherSpawnMultiplier",
            1f,
            "Other Spawn Multiplier",
            "Spawn multiplier for entities that are not mimics, bosses, jakos, specials, or traps.");

        AutoScaleOtherSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleOtherSpawnsByPlayerCount",
            true,
            "Auto Scale Other Spawns By Player Count",
            "When enabled, multiply other spawn counts by player count / 4 for sessions with more than 4 players (stacks with OtherSpawnMultiplier).");

        EnableDebugLogging = Category.CreateEntry(
            "EnableDebugLogging",
            false,
            "Enable Debug Logging",
            "Emit verbose diagnostic lines to the MelonLoader console.");

        if (MaxPlayers.Value < 1)
        {
            logger.Warning("MaxPlayers must be at least 1; resetting to 1.");
            MaxPlayers.Value = 1;
        }

        MaxPlayers.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("MaxPlayers must be at least 1; resetting to 1.");
                MaxPlayers.Value = 1;
                return;
            }

            NotifyChanged();
        });

        MaxVoiceEvents.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("MaxVoiceEvents must be at least 1; resetting to 1.");
                MaxVoiceEvents.Value = 1;
                return;
            }

            NotifyChanged();
        });

        EnableMorePlayers.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        EnableMoreVoices.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        EnablePersistence.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

        SessionReconnectGraceMinutes.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("SessionReconnectGraceMinutes must be at least 1; resetting to 1.");
                SessionReconnectGraceMinutes.Value = 1;
                return;
            }

            NotifyChanged();
        });

        EnableStatistics.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        ShowStatisticsToasts.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        EnableJoinAnytime.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        EnableSpawnScaling.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleMimicSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleBossSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleJakoSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleSpecialSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleTrapSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        FixedSpawnRespawnDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnFixedSpawnRespawnDelayChanged(logger, value, FixedSpawnRespawnDelayMinSeconds));
        FixedSpawnRespawnDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnFixedSpawnRespawnDelayChanged(logger, value, FixedSpawnRespawnDelayMaxSeconds));
        AutoScaleOtherSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

        MimicSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, MimicSpawnMultiplier));
        BossSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, BossSpawnMultiplier));
        JakoSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, JakoSpawnMultiplier));
        SpecialSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, SpecialSpawnMultiplier));
        TrapSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, TrapSpawnMultiplier));
        OtherSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, OtherSpawnMultiplier));

        EnableDebugLogging.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

        IsInitialized = true;
    }

    private static void OnFixedSpawnRespawnDelayChanged(MelonLogger.Instance logger, float value, MelonPreferences_Entry<float> entry)
    {
        if (value < 0f)
        {
            logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
            entry.Value = 0f;
            return;
        }

        float min = FixedSpawnRespawnDelayMinSeconds.Value;
        float max = FixedSpawnRespawnDelayMaxSeconds.Value;
        if (max < min)
        {
            logger.Warning("FixedSpawnRespawnDelayMaxSeconds must be >= FixedSpawnRespawnDelayMinSeconds; syncing max to min.");
            FixedSpawnRespawnDelayMaxSeconds.Value = min;
        }

        NotifyChanged();
    }

    private static void OnSpawnMultiplierChanged(MelonLogger.Instance logger, float value, MelonPreferences_Entry<float> entry)
    {
        if (value < 0f)
        {
            logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
            entry.Value = 0f;
            return;
        }

        NotifyChanged();
    }

    private static void NotifyChanged() => Changed?.Invoke();
}
