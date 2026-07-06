namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherCycleScheduler
    {
        private const string Feature = "Weather";

        internal static void StartCycle(DungeonRoom room)
        {
            WeatherRoomState state = WeatherRoomAccess.GetOrCreateState(room);
            state.CycleIndex = 0;
            state.CycleActive = true;
            ApplyCurrentCycleStep(room, state, scheduleNext: true);
        }

        internal static void Stop(DungeonRoom room)
        {
            WeatherRoomState state = WeatherRoomAccess.GetOrCreateState(room);
            state.CycleActive = false;
            state.NextTransitionTickMs = 0;
        }

        internal static void RestartFromConfig(DungeonRoom room)
        {
            if (WeatherResolver.GetMode() != WeatherMode.Cycle)
            {
                Stop(room);
                return;
            }

            List<string> presets = WeatherPresetListParser.ParseOrderedPresets(ModConfig.WeatherCyclePresets.Value);
            if (presets.Count == 0)
            {
                Stop(room);
                return;
            }

            WeatherRoomState state = WeatherRoomAccess.GetOrCreateState(room);
            if (state.CycleIndex >= presets.Count)
            {
                state.CycleIndex = 0;
            }

            state.CycleActive = true;
            ApplyCurrentCycleStep(room, state, scheduleNext: true);
        }

        internal static void ProcessPendingTransitions()
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature(() => WeatherResolver.IsFeatureEnabled))
            {
                return;
            }

            if (WeatherResolver.GetMode() != WeatherMode.Cycle)
            {
                return;
            }

            long nowMs = GameSessionAccess.TryGetTimeUtil()?.GetCurrentTickMilliSec() ?? 0;
            foreach (KeyValuePair<DungeonRoom, WeatherRoomState> entry in WeatherRoomAccess.RoomStates.EnumerateAll())
            {
                DungeonRoom room = entry.Key;
                WeatherRoomState state = entry.Value;
                if (!state.CycleActive || !WeatherRoomAccess.IsPlaying(room))
                {
                    continue;
                }

                if (state.NextTransitionTickMs <= 0 || nowMs < state.NextTransitionTickMs)
                {
                    continue;
                }

                List<string> presets = WeatherPresetListParser.ParseOrderedPresets(ModConfig.WeatherCyclePresets.Value);
                if (presets.Count == 0)
                {
                    Stop(room);
                    continue;
                }

                state.CycleIndex = (state.CycleIndex + 1) % presets.Count;
                ApplyCurrentCycleStep(room, state, scheduleNext: true);
            }
        }

        private static void ApplyCurrentCycleStep(DungeonRoom room, WeatherRoomState state, bool scheduleNext)
        {
            List<string> presets = WeatherPresetListParser.ParseOrderedPresets(ModConfig.WeatherCyclePresets.Value);
            if (presets.Count == 0)
            {
                Stop(room);
                return;
            }

            if (state.CycleIndex >= presets.Count)
            {
                state.CycleIndex = 0;
            }

            string presetName = presets[state.CycleIndex];
            if (!WeatherResolver.TryResolvePresetMasterId(presetName, out int masterId))
            {
                ModLog.Warn(Feature, $"Cycle preset unresolved — {presetName}");
                return;
            }

            room.AdminChangeWeather(masterId);
            WeatherLog.InfoCycleTransition(state.CycleIndex, presetName, masterId);

            if (scheduleNext && presets.Count > 1)
            {
                WeatherResolver.GetCycleDelayRange(out float minSeconds, out float maxSeconds);
                float delaySeconds = minSeconds >= maxSeconds
                    ? minSeconds
                    : SimpleRandUtil.Next((int)(minSeconds * 1000f), (int)(maxSeconds * 1000f) + 1) / 1000f;
                state.NextTransitionTickMs = (GameSessionAccess.TryGetTimeUtil()?.GetCurrentTickMilliSec() ?? 0)
                    + (long)(delaySeconds * 1000d);
            }
            else
            {
                state.NextTransitionTickMs = 0;
            }
        }
    }
}
