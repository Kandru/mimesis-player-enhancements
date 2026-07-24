namespace MimesisPlayerEnhancement.Features.Weather.Patches
{
    // game@0.3.1 Assembly-CSharp/VWorldUtil.cs:L102-109
    [HarmonyPatch(typeof(VWorldUtil), nameof(VWorldUtil.ConvertTimeToSeconds), [typeof(string)])]
    internal static class VWorldUtilConvertTimeToSecondsPatch
    {
        private const string Feature = "Weather";

        [HarmonyPostfix]
        public static void Postfix(ref long __result)
        {
            try
            {
                if (!WeatherTimeContext.TryGetActiveRoom(out DungeonRoom room))
                {
                    return;
                }

                if (!HostApplyGate.ShouldApplyHostOnlyFeature(() => WeatherTimeResolver.UsesOverrideStartTime()))
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
}
