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
            "Show join/leave/cycle messages in the bottom-left player info UI.");

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
        EnableDebugLogging.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

        IsInitialized = true;
    }

    private static void NotifyChanged() => Changed?.Invoke();
}
