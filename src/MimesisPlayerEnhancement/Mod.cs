using System;
using MelonLoader;

[assembly: MelonInfo(typeof(MimesisPlayerEnhancement.Mod), "MimesisPlayerEnhancement", MimesisPlayerEnhancement.VersionInfo.ModuleVersion, "kalle")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]
[assembly: HarmonyDontPatchAll]

namespace MimesisPlayerEnhancement;

public sealed class Mod : MelonMod
{
    private HarmonyLib.Harmony? _harmony;

    public override void OnInitializeMelon()
    {
        ModConfig.Initialize(LoggerInstance);
        ModConfig.Changed += SyncFromConfig;

        _harmony = new HarmonyLib.Harmony("com.mimesis.playerenhancement");
        // Apply compile-time patches first so Assembly-CSharp is loaded before MorePlayers resolves targets.
        Features.MoreVoices.MoreVoicesPatches.Apply(_harmony);
        Features.Persistence.PersistencePatches.Apply(_harmony);
        Features.MorePlayers.MorePlayersPatches.Apply(_harmony);

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
    }

    public override void OnDeinitializeMelon()
    {
        ModConfig.Changed -= SyncFromConfig;
        _harmony?.UnpatchSelf();
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

        ModLog.Debug(
            "Config",
            $"Synced — MorePlayers={ModConfig.EnableMorePlayers.Value} (session cap {sessionCap}), " +
            $"MoreVoices={ModConfig.EnableMoreVoices.Value} (max {ModConfig.MaxVoiceEvents.Value}), " +
            $"Persistence={ModConfig.EnablePersistence.Value}, " +
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
            $", Persistence={ModConfig.EnablePersistence.Value}, " +
            $"DebugLogging={ModConfig.EnableDebugLogging.Value}");
    }
}
