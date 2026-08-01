namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeClockResolver
    {
        internal static bool UsesOverrideStartTime() =>
            UsesOverrideStartTime(SceneScopedConfigGate.DungeonTime);

        internal static bool UsesOverrideStartTime(DungeonTimeSceneConfig config) =>
            config.EnableDungeonTime
            && config.StartTimePreset != StartTimePreset.Vanilla;

        internal static StartTimePreset ParseStartTimePreset(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)
                || string.Equals(value, "Vanilla", StringComparison.OrdinalIgnoreCase))
            {
                return StartTimePreset.Vanilla;
            }

            if (Enum.TryParse(value.Trim(), ignoreCase: true, out StartTimePreset preset))
            {
                return preset;
            }

            return StartTimePreset.Vanilla;
        }

        internal static bool TryGetPresetHour(StartTimePreset preset, out int hour)
        {
            hour = preset switch
            {
                StartTimePreset.Morning => 8,
                StartTimePreset.Noon => 12,
                StartTimePreset.Dusk => 18,
                StartTimePreset.Night => 21,
                StartTimePreset.Midnight => 0,
                _ => -1,
            };
            return hour >= 0;
        }

        internal static long GetEffectiveStartSeconds(DungeonRoom room) =>
            GetEffectiveStartSeconds(room, SceneScopedConfigGate.DungeonTime);

        internal static long GetEffectiveStartSeconds(DungeonRoom room, DungeonTimeSceneConfig config)
        {
            long vanilla = DungeonTimeRoomAccess.GetVanillaStartSeconds(room);
            if (!UsesOverrideStartTime(config) || !TryGetPresetHour(config.StartTimePreset, out int hour))
            {
                return vanilla;
            }

            return hour * 3600L;
        }

        internal static TimeSpan ComputeDisplayTime(DungeonRoom room) =>
            ComputeDisplayTime(room, SceneScopedConfigGate.DungeonTime);

        internal static TimeSpan ComputeDisplayTime(DungeonRoom room, DungeonTimeSceneConfig config)
        {
            double elapsed = DungeonTimeRoomAccess.GetElapsedGameSeconds(room);
            long startSeconds = GetEffectiveStartSeconds(room, config);
            return TimeSpan.FromSeconds(elapsed + startSeconds);
        }
    }
}
