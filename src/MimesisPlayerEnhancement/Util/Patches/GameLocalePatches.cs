namespace MimesisPlayerEnhancement.Util.Patches
{
    internal static class GameLocalePatches
    {
        private const string Feature = "Locale";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(GameLocalePatches)));
        }

        // game@0.3.1 Assembly-CSharp/L10NManager.cs:L80-97
        [HarmonyPatch(typeof(L10NManager), nameof(L10NManager.ChangeLanguage))]
        internal static class L10NManagerChangeLanguagePostfix
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                try
                {
                    GameLocaleAccess.NotifyLanguageChanged();
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ChangeLanguage postfix failed — {ex.Message}");
                }
            }
        }
    }
}
