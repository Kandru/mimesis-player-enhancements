using System;
using System.Collections.Generic;

namespace MimesisPlayerEnhancement.Features.Statistics.Models;

public sealed class StatCounters
{
    public long ItemCarryCount;
    public long DamageToAlly;
    public long MimicEncounterCount;
    public long TimeInStartingVolumeMs;
    public long CurrencyEarned;
    public long VoiceEvents;
    public long Deaths;
    public long Revives;
    public long Kills;
    public int CyclesCompleted;
    public long TotalConnectedSeconds;

    public void Add(StatCounters other)
    {
        if (other == null) return;
        ItemCarryCount += other.ItemCarryCount;
        DamageToAlly += other.DamageToAlly;
        MimicEncounterCount += other.MimicEncounterCount;
        TimeInStartingVolumeMs += other.TimeInStartingVolumeMs;
        CurrencyEarned += other.CurrencyEarned;
        VoiceEvents += other.VoiceEvents;
        Deaths += other.Deaths;
        Revives += other.Revives;
        Kills += other.Kills;
        CyclesCompleted += other.CyclesCompleted;
        TotalConnectedSeconds += other.TotalConnectedSeconds;
    }

    public StatCounters Clone() => new StatCounters
    {
        ItemCarryCount = ItemCarryCount,
        DamageToAlly = DamageToAlly,
        MimicEncounterCount = MimicEncounterCount,
        TimeInStartingVolumeMs = TimeInStartingVolumeMs,
        CurrencyEarned = CurrencyEarned,
        VoiceEvents = VoiceEvents,
        Deaths = Deaths,
        Revives = Revives,
        Kills = Kills,
        CyclesCompleted = CyclesCompleted,
        TotalConnectedSeconds = TotalConnectedSeconds,
    };
}

public sealed class SessionStats
{
    public string SessionId = "";
    public DateTime StartedAtUtc;
    public DateTime LastConnectedAtUtc;
    public DateTime? LastDisconnectedAtUtc;
    public int ReconnectCount;
    public bool IsOpen = true;
    public StatCounters Counters = new();
}

public sealed class GlobalStats
{
    public StatCounters Counters = new();
    public int SessionsCompleted;
}

public sealed class PlayerStatisticsDocument
{
    public const int CurrentVersion = 1;

    public int Version = CurrentVersion;
    public ulong SteamId;
    public string DisplayName = "";
    public GlobalStats Global = new();
    public SessionStats? CurrentSession;
    public List<SessionStats> RecentSessions = new();
}

public sealed class LeaderboardEntry
{
    public ulong SteamId;
    public string DisplayName = "";
    public long ItemCarryCount;
    public long DamageToAlly;
    public long MimicEncounterCount;
    public long TimeInStartingVolumeMs;
    public long CurrencyEarned;
    public long VoiceEvents;
    public long Deaths;
    public long Revives;
    public long Kills;
    public long TotalConnectedSeconds;
    public int SessionsCompleted;
}

public sealed class LeaderboardDocument
{
    public const int CurrentVersion = 1;

    public int Version = CurrentVersion;
    public int SaveSlotId;
    public DateTime UpdatedAtUtc;
    public List<LeaderboardEntry> Entries = new();
}
