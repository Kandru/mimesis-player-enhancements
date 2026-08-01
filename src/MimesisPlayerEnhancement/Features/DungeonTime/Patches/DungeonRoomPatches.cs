namespace MimesisPlayerEnhancement.Features.DungeonTime.Patches
{
    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L100-119
    [HarmonyPatch(typeof(DungeonRoom), MethodType.Constructor, [typeof(VRoomManager), typeof(long), typeof(IVRoomProperty)])]
    internal static class DungeonRoomConstructorPatch
    {
        private const string Feature = "DungeonTime";

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance)
        {
            try
            {
                DungeonTimeRoomState state = DungeonTimeRoomAccess.GetOrCreateState(__instance);
                state.VanillaStartSeconds = DungeonTimeRoomAccess.CaptureVanillaStartSeconds(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"DungeonRoom ctor postfix failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L1018-1021
    [HarmonyPatch(typeof(DungeonRoom), "OnAllMemberEntered")]
    internal static class DungeonRoomOnAllMemberEnteredPatch
    {
        private const string Feature = "DungeonTime";

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance)
        {
            try
            {
                DungeonTimeApplier.EnsureApplied(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnAllMemberEntered postfix failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L871-876
    [HarmonyPatch(typeof(DungeonRoom), "GetCurrentTime")]
    internal static class DungeonRoomGetCurrentTimePatch
    {
        private const string Feature = "DungeonTime";

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance, ref TimeSpan __result)
        {
            try
            {
                if (!HostApplyGate.ShouldApplyHostOnlyFeature(() => DungeonTimeClockResolver.UsesOverrideStartTime()))
                {
                    return;
                }

                __result = DungeonTimeClockResolver.ComputeDisplayTime(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"GetCurrentTime postfix failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L707-766
    // Shrink the pending _elapsedTime add so display/hourly TimeSyncSig span the extended real shift.
    // Do not send extra TimeSyncSig — clients set worldTime = Hours and locally advance at vanilla
    // scale between packets, so sub-hour syncs snap the sky backward.
    [HarmonyPatch(typeof(DungeonRoom), "OnUpdate")]
    [HarmonyPriority(HarmonyLib.Priority.First)]
    internal static class DungeonRoomOnUpdateDisplayScalePatch
    {
        private const string Feature = "DungeonTime";

        [HarmonyPrefix]
        public static void Prefix(DungeonRoom __instance, long delta)
        {
            try
            {
                _ = DungeonTimeRuntime.TryPrepareElapsedForScaledDelta(__instance, delta);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnUpdate display-scale prefix failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L705-748
    [HarmonyPatch(typeof(DungeonRoom), "OnUpdate")]
    internal static class DungeonRoomOnUpdateClockPatch
    {
        private const string Feature = "DungeonTime";

        // Runs every dungeon frame — only when start-time override or realtime tram clock can run.
        private static bool IsContextNeeded
        {
            get
            {
                DungeonTimeSceneConfig config = SceneScopedConfigGate.DungeonTime;
                return HostApplyGate.ShouldApplyHostOnlyFeature(() =>
                    DungeonTimeClockResolver.UsesOverrideStartTime(config)
                    || (config.EnableDungeonTime && config.EnableRealtimeTramClock));
            }
        }

        [HarmonyPrefix]
        public static void Prefix(DungeonRoom __instance)
        {
            if (!IsContextNeeded)
            {
                return;
            }

            DungeonTimeClockContext.Enter(__instance);
        }

        [HarmonyFinalizer]
        public static Exception? Finalizer(Exception? __exception)
        {
            DungeonTimeClockContext.Exit();
            return __exception;
        }

        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance)
        {
            if (!IsContextNeeded)
            {
                return;
            }

            DungeonTimeTramClockSync.TrySyncFromUpdate(__instance);
        }
    }
}
