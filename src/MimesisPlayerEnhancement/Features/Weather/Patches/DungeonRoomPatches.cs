namespace MimesisPlayerEnhancement.Features.Weather.Patches
{
    [HarmonyPatch(typeof(DungeonRoom), MethodType.Constructor, [typeof(VRoomManager), typeof(long), typeof(IVRoomProperty)])]
    internal static class DungeonRoomConstructorPatch
    {
        private const string Feature = "Weather";

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance, IVRoomProperty property)
        {
            try
            {
                if (!WeatherRoomAccess.TryGetWeather(__instance, out DungeonWeather? weather) || weather == null)
                {
                    return;
                }

                WeatherRoomState state = WeatherRoomAccess.GetOrCreateState(__instance);
                state.VanillaSnapshot = WeatherRoomAccess.CaptureWeatherSnapshot(__instance, weather);
                if (property is DungeonProperty dungeonProperty)
                {
                    state.VanillaSnapshot.DayCount = dungeonProperty.CycleCount;
                    state.VanillaSnapshot.RandomSeed = dungeonProperty.RandomDungeonSeed;
                }

                state.VanillaSnapshot.OverrideDefaultWeatherId =
                    GameSessionAccess.TryGetGameSessionInfo()?.OverrideDefaultWeatherID ?? 0;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"DungeonRoom ctor postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(DungeonRoom), "OnAllMemberEntered")]
    internal static class DungeonRoomOnAllMemberEnteredPatch
    {
        private const string Feature = "Weather";

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance)
        {
            try
            {
                WeatherApplier.EnsureApplied(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnAllMemberEntered postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(DungeonRoom), "GetCurrentTime")]
    internal static class DungeonRoomGetCurrentTimePatch
    {
        private const string Feature = "Weather";

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance, ref TimeSpan __result)
        {
            try
            {
                if (!WeatherTimeResolver.UsesOverrideStartTime())
                {
                    return;
                }

                __result = WeatherTimeResolver.ComputeDisplayTime(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"GetCurrentTime postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(DungeonRoom), "OnUpdate")]
    internal static class DungeonRoomOnUpdatePatch
    {
        private const string Feature = "Weather";

        // Runs every dungeon frame — only maintain the time context when a weather
        // override (or the realtime tram clock) can actually consume it.
        private static bool IsContextNeeded =>
            WeatherResolver.IsFeatureEnabled || ModConfig.EnableRealtimeTramClock.Value;

        [HarmonyPrefix]
        public static void Prefix(DungeonRoom __instance)
        {
            if (!IsContextNeeded)
            {
                return;
            }

            WeatherTimeContext.Enter(__instance);
        }

        [HarmonyFinalizer]
        public static Exception? Finalizer(Exception? __exception)
        {
            WeatherTimeContext.Exit();
            return __exception;
        }

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance)
        {
            if (!IsContextNeeded)
            {
                return;
            }

            WeatherTramClockSync.TrySyncFromUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(DungeonRoom), "SetDungeonState")]
    internal static class DungeonRoomSetDungeonStatePatch
    {
        private const string Feature = "Weather";

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance, DungeonState state)
        {
            try
            {
                if (state != DungeonState.Success && state != DungeonState.Failed)
                {
                    return;
                }

                WeatherCycleScheduler.Stop(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SetDungeonState postfix failed — {ex.Message}");
            }
        }
    }
}
