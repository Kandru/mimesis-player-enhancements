namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherPatches
    {
        private const string Feature = "Weather";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(WeatherPatches)));

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
    }
}
