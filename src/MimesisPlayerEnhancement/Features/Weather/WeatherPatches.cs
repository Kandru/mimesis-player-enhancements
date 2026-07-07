namespace MimesisPlayerEnhancement.Features.Weather
{
    public static class WeatherPatches
    {
        private const string Feature = "Weather";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(WeatherPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        public static void RefreshFromConfig() => WeatherRuntime.RefreshFromConfig();

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("DungeonRoom..ctor", AccessTools.Constructor(typeof(DungeonRoom), [typeof(VRoomManager), typeof(long), typeof(IVRoomProperty)])),
                ("DungeonWeather..ctor", AccessTools.Constructor(typeof(DungeonWeather), [typeof(int), typeof(int), typeof(int)])),
                ("OnAllMemberEntered/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "OnAllMemberEntered")),
                ("GetCurrentTime/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "GetCurrentTime")),
                ("OnUpdate/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "OnUpdate")),
                ("SetDungeonState/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "SetDungeonState")),
                ("ConvertTimeToSeconds/VWorldUtil", AccessTools.Method(typeof(VWorldUtil), nameof(VWorldUtil.ConvertTimeToSeconds), [typeof(string)])),
            ]);
        }

        [HarmonyPatch(typeof(DungeonRoom), MethodType.Constructor, [typeof(VRoomManager), typeof(long), typeof(IVRoomProperty)])]
        public static class DungeonRoomConstructorPatch
        {
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

        [HarmonyPatch(typeof(DungeonWeather), MethodType.Constructor, [typeof(int), typeof(int), typeof(int)])]
        public static class DungeonWeatherConstructorPatch
        {
            [HarmonyPostfix]
            public static void Postfix(DungeonWeather __instance, int dayCount, int randomSeed, int overrideDefaultWeatherID)
            {
                try
                {
                    if (WeatherResolver.ShouldStripRandomWeather() && __instance.IsRandomOccured)
                    {
                        WeatherScheduleRebuilder.StripRandomWeather(
                            __instance,
                            dayCount,
                            randomSeed,
                            overrideDefaultWeatherID);
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"DungeonWeather ctor postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(DungeonRoom), "OnAllMemberEntered")]
        public static class DungeonRoomOnAllMemberEnteredPatch
        {
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
        public static class DungeonRoomGetCurrentTimePatch
        {
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
        public static class DungeonRoomOnUpdatePatch
        {
            [HarmonyPrefix]
            public static void Prefix(DungeonRoom __instance)
            {
                WeatherTimeContext.Enter(__instance);
            }

            [HarmonyFinalizer]
            public static Exception? Finalizer(Exception? __exception)
            {
                WeatherTimeContext.Exit();
                return __exception;
            }
        }

        [HarmonyPatch(typeof(VWorldUtil), nameof(VWorldUtil.ConvertTimeToSeconds), [typeof(string)])]
        public static class VWorldUtilConvertTimeToSecondsPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref long __result)
            {
                try
                {
                    if (!WeatherTimeContext.TryGetActiveRoom(out DungeonRoom room))
                    {
                        return;
                    }

                    if (!WeatherTimeResolver.UsesOverrideStartTime())
                    {
                        return;
                    }

                    // Only override dungeon start-display lookups (e.g. "10:00:00"), not arbitrary times.
                    if (!WeatherTimeContext.ShouldOverrideConvertResult(__result))
                    {
                        return;
                    }

                    __result = WeatherTimeResolver.GetEffectiveStartSeconds(room);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ConvertTimeToSeconds postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(DungeonRoom), "SetDungeonState")]
        public static class DungeonRoomSetDungeonStatePatch
        {
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
}
