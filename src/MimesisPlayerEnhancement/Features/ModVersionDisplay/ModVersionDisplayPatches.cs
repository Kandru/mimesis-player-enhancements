using System.Reflection;

namespace MimesisPlayerEnhancement.Features.ModVersionDisplay
{
    public static class ModVersionDisplayPatches
    {
        private const string Feature = "ModVersionDisplay";
        private const BindingFlags VersionTextBinding = BindingFlags.Instance | BindingFlags.Public;

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(ModVersionDisplayPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("SetVersionText/UIPrefab_MainMenu", AccessTools.Method(typeof(UIPrefab_MainMenu), "SetVersionText")),
                ("SetVersionText/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), "SetVersionText")),
            ]);
        }

        internal static void PrependModVersion(object uiPrefab)
        {
            PropertyInfo? versionTextProp = uiPrefab.GetType().GetProperty("UE_versionText", VersionTextBinding);
            object? versionText = versionTextProp?.GetValue(uiPrefab);
            if (versionText == null)
            {
                return;
            }

            PropertyInfo? textProp = versionText.GetType().GetProperty("text");
            if (textProp == null || textProp.PropertyType != typeof(string))
            {
                return;
            }

            string current = textProp.GetValue(versionText) as string ?? string.Empty;
            string prefix = $"MimesisPlayerEnhancement v{VersionInfo.ModuleVersion}";
            if (current.StartsWith(prefix, StringComparison.Ordinal))
            {
                return;
            }

            textProp.SetValue(
                versionText,
                $"{prefix}\n{current}");
        }

        [HarmonyPatch(typeof(UIPrefab_MainMenu), "SetVersionText")]
        public static class UIPrefabMainMenuSetVersionTextPatch
        {
            [HarmonyPostfix]
            public static void Postfix(UIPrefab_MainMenu __instance)
            {
                try
                {
                    PrependModVersion(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Main menu version overlay failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(UIPrefab_InGameMenu), "SetVersionText")]
        public static class UIPrefabInGameMenuSetVersionTextPatch
        {
            [HarmonyPostfix]
            public static void Postfix(UIPrefab_InGameMenu __instance)
            {
                try
                {
                    PrependModVersion(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"In-game menu version overlay failed — {ex.Message}");
                }
            }
        }
    }
}
