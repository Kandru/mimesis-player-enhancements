namespace MimesisPlayerEnhancement.Features.Weather.Patches
{
    // game@0.3.1 Assembly-CSharp/DungeonWeather.cs:L31-133
    [HarmonyPatch(typeof(DungeonWeather), MethodType.Constructor, [typeof(int), typeof(int), typeof(int)])]
    internal static class DungeonWeatherConstructorPatch
    {
        private const string Feature = "Weather";

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
}
