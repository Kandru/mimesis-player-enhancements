namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherPatches
    {
        private const string Feature = "Weather";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(WeatherPatches)));
        }

        public static void RefreshFromConfig() => WeatherRuntime.RefreshFromConfig();
    }
}
