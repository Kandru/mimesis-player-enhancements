using System;

namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherTimeResolver
    {
        internal static bool UsesOverrideStartTime() =>
            WeatherResolver.IsFeatureEnabled
            && ParseStartTimePreset(ModConfig.StartTimePreset.Value) != StartTimePreset.Vanilla;

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

        internal static long GetEffectiveStartSeconds(DungeonRoom room)
        {
            long vanilla = WeatherRoomAccess.GetVanillaStartSeconds(room);
            if (!UsesOverrideStartTime())
            {
                return vanilla;
            }

            StartTimePreset preset = ParseStartTimePreset(ModConfig.StartTimePreset.Value);
            if (!TryGetPresetHour(preset, out int hour))
            {
                return vanilla;
            }

            return hour * 3600L;
        }

        internal static TimeSpan ComputeDisplayTime(DungeonRoom room)
        {
            double elapsed = WeatherRoomAccess.GetElapsedGameSeconds(room);
            long startSeconds = GetEffectiveStartSeconds(room);
            return TimeSpan.FromSeconds(elapsed + startSeconds);
        }
    }
}
