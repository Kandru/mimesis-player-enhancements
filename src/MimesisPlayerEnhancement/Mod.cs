using System;
using MelonLoader;

[assembly: MelonInfo(typeof(MimesisPlayerEnhancement.Mod), "MimesisPlayerEnhancement", MimesisPlayerEnhancement.VersionInfo.ModuleVersion, "kalle")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]
[assembly: HarmonyDontPatchAll]

namespace MimesisPlayerEnhancement;

public sealed class Mod : MelonMod
{
    private HarmonyLib.Harmony? _harmony;
    private bool _statisticsWasEnabled;

    public override void OnInitializeMelon()
    {
        ModConfig.Initialize(LoggerInstance);
        ModConfig.Changed += SyncFromConfig;

        _harmony = new HarmonyLib.Harmony("com.mimesis.playerenhancement");
        // Apply compile-time patches first so Assembly-CSharp is loaded before MorePlayers resolves targets.
        Features.MoreVoices.MoreVoicesPatches.Apply(_harmony);
        Features.Persistence.PersistencePatches.Apply(_harmony);
        Features.Statistics.StatisticsPatches.Apply(_harmony);
        Features.MorePlayers.MorePlayersPatches.Apply(_harmony);
        Features.JoinAnytime.JoinAnytimePatches.Apply(_harmony);
        Features.SpawnScaling.SpawnScalingPatches.Apply(_harmony);

        _statisticsWasEnabled = ModConfig.EnableStatistics.Value;
        SyncFromConfig();
        LogStartupSummary();
    }

    public override void OnPreferencesSaved(string filepath)
    {
        if (IsOurConfigFile(filepath))
            SyncFromConfig();
    }

    public override void OnPreferencesLoaded(string filepath)
    {
        if (IsOurConfigFile(filepath))
            SyncFromConfig();
    }

    public override void OnUpdate()
    {
        if (ModConfig.EnablePersistence.Value)
            Features.Persistence.SpeechEventPoolManager.ProcessDeferredUpdates();

        if (ModConfig.EnableStatistics.Value)
            Features.Statistics.StatisticsTracker.OnUpdate();

        if (ModConfig.EnableSpawnScaling.Value)
            Features.SpawnScaling.FixedSpawnCoordinator.ProcessPendingRespawns();
    }

    public override void OnDeinitializeMelon()
    {
        ModConfig.Changed -= SyncFromConfig;
        if (_harmony != null)
        {
            _harmony.UnpatchSelf();
            ModLog.Debug("Startup", "Harmony patches removed.");
        }
    }

    private static bool IsOurConfigFile(string filepath) =>
        string.Equals(filepath, ModConfig.FilePath, StringComparison.OrdinalIgnoreCase);

    private void SyncFromConfig()
    {
        if (!ModConfig.IsInitialized)
            return;

        Features.MorePlayers.MorePlayersPatches.RefreshFromConfig();
        Features.MoreVoices.MoreVoicesPatches.RefreshFromConfig();

        int sessionCap = ModConfig.EnableMorePlayers.Value ? ModConfig.MaxPlayers.Value : 4;

        if (_statisticsWasEnabled && !ModConfig.EnableStatistics.Value)
            Features.Statistics.StatisticsTracker.ClearRuntimeState();

        _statisticsWasEnabled = ModConfig.EnableStatistics.Value;

        ModLog.Debug(
            "Config",
            $"Synced — MorePlayers={ModConfig.EnableMorePlayers.Value} (session cap {sessionCap}), " +
            $"MoreVoices={ModConfig.EnableMoreVoices.Value} (max {ModConfig.MaxVoiceEvents.Value}), " +
            $"Persistence={ModConfig.EnablePersistence.Value}, " +
            $"Statistics={ModConfig.EnableStatistics.Value}, " +
            $"JoinAnytime={ModConfig.EnableJoinAnytime.Value}, " +
            $"SpawnScaling={ModConfig.EnableSpawnScaling.Value}, " +
            $"DebugLogging={ModConfig.EnableDebugLogging.Value}");
    }

    private void LogStartupSummary()
    {
        ModLog.Info(
            "Startup",
            $"v{VersionInfo.ModuleVersion} loaded — " +
            $"MorePlayers={ModConfig.EnableMorePlayers.Value}" +
            (ModConfig.EnableMorePlayers.Value ? $" (session cap {ModConfig.MaxPlayers.Value})" : "") +
            $", MoreVoices={ModConfig.EnableMoreVoices.Value}" +
            (ModConfig.EnableMoreVoices.Value ? $" (max {ModConfig.MaxVoiceEvents.Value})" : "") +
            $", Persistence={ModConfig.EnablePersistence.Value}" +
            $", Statistics={ModConfig.EnableStatistics.Value}, " +
            $"JoinAnytime={ModConfig.EnableJoinAnytime.Value}, " +
            $"SpawnScaling={ModConfig.EnableSpawnScaling.Value}, " +
            $"DebugLogging={ModConfig.EnableDebugLogging.Value}");
    }
}
