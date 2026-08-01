namespace MimesisPlayerEnhancement.Features.DungeonTime.Patches
{
    // game@0.3.1 Assembly-CSharp/GamePlayScene.cs:L1153-1167
    [HarmonyPatch(typeof(GamePlayScene), "OnTimeChanged")]
    internal static class GamePlaySceneOnTimeChangedPatch
    {
        private const string Feature = "DungeonTime";

        [HarmonyPostfix]
        public static void Postfix(TimeSpan currentTime)
        {
            try
            {
                DungeonTimeClientWorldClock.OnTimeSync(currentTime);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnTimeChanged client clock failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/SkyAndWeatherSystem.cs:L489
    [HarmonyPatch(typeof(SkyAndWeatherSystem), nameof(SkyAndWeatherSystem.SetTime))]
    internal static class SkyAndWeatherSystemSetTimePatch
    {
        private const string Feature = "DungeonTime";

        [HarmonyPrefix]
        public static void Prefix(ref float newHours)
        {
            try
            {
                if (GameSessionAccess.TryGetPdata()?.main is not GamePlayScene scene)
                {
                    return;
                }

                DungeonTimeClientWorldClock.ApplyToSetTime(scene, ref newHours);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SetTime client clock failed — {ex.Message}");
            }
        }
    }
}
