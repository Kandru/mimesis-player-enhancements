using System.Linq;
using System.Reflection;
using MelonLoader.Logging;

namespace MimesisPlayerEnhancement.Util
{
    internal static class HarmonyPatchHelper
    {
        internal struct PatchApplyResult
        {
            internal int Applied;
            internal int Failed;
        }

        internal static IEnumerable<Type> GetNestedPatchTypes(Type containerType)
        {
            return containerType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);
        }

        internal static IEnumerable<Type> GetNamespacePatchTypes(Type anchorType, string suffix = ".Patches")
        {
            return anchorType.Assembly.GetTypes()
                .Where(t => t.Namespace == anchorType.Namespace + suffix
                            && t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);
        }

        internal static PatchApplyResult ApplyPatchTypes(HarmonyLib.Harmony harmony, string feature, IEnumerable<Type> patchTypes)
        {
            int applied = 0;
            int failed = 0;

            foreach (Type patchType in patchTypes)
            {
                try
                {
                    List<MethodInfo> results = harmony.CreateClassProcessor(patchType).Patch();
                    if (results != null && results.Count > 0)
                    {
                        applied += results.Count;
                    }
                    else
                    {
                        failed++;
                        if (patchType.GetMethod("TargetMethod", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null)
                        {
                            ModLog.Warn(feature, $"Patch {patchType.Name} — TargetMethod returned no patches");
                        }
                        else
                        {
                            ModLog.Warn(feature, $"Patch class {patchType.Name} did not apply to any methods.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    ModLog.Warn(feature, $"Patch {patchType.Name} failed — {ex.Message}");
                }
            }

            return new PatchApplyResult { Applied = applied, Failed = failed };
        }

        internal static void LogPatchSummary(string feature, PatchApplyResult result)
        {
            string message = $"{result.Applied} patch(es), {result.Failed} failure(s).";
            ColorARGB? lineColor = PickOutcomeLineColor(result.Applied, result.Failed);

            ModLog.PassLogSegmented(ModLog.FeatureSection(feature, "Patches Applied"), message, (lineColor, message));
        }

        internal static void LogPatchAudit(string feature, HarmonyLib.Harmony harmony, IEnumerable<(string label, MethodBase? method)> checks)
        {
            if (!ModConfig.EnableDebugLogging.Value)
            {
                return;
            }

            List<(string text, bool ok)> entries = [];

            foreach ((string? label, MethodBase? method) in checks)
            {
                bool ok = method != null && IsPatched(harmony, method);
                string text = method == null ? $"{label} (not found)" : ok ? $"{label} (ok)" : $"{label} (miss)";
                entries.Add((text, ok));
            }

            if (entries.Count == 0)
            {
                return;
            }

            int missed = entries.Count(e => !e.ok);
            string message = string.Join(", ", entries.Select(e => e.text));
            ColorARGB? lineColor = PickOutcomeLineColor(entries.Count - missed, missed);

            ModLog.PassLogSegmented(ModLog.FeatureSection(feature, "Patch Audit"), message, (lineColor, message));
        }

        private static ColorARGB? PickOutcomeLineColor(int succeeded, int failed)
        {
            if (failed == 0)
            {
                return succeeded > 0 ? ModLog.SuccessGreen : null;
            }

            return succeeded > 0 ? ModLog.PartialYellow : ModLog.FailureRed;
        }

        /// <summary>
        /// Returns the compiler-generated MoveNext for an iterator method (IEnumerator/async).
        /// Transpilers must patch MoveNext — not the outer iterator stub.
        /// </summary>
        internal static MethodInfo? GetEnumeratorMoveNext(Type declaringType, string methodName, Type[]? parameters = null)
        {
            MethodInfo? iterator = parameters == null
                ? AccessTools.Method(declaringType, methodName)
                : AccessTools.Method(declaringType, methodName, parameters);
            if (iterator == null)
            {
                return null;
            }

            string prefix = "<" + methodName + ">";
            foreach (Type nested in declaringType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!nested.Name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                MethodInfo? moveNext = AccessTools.Method(nested, "MoveNext");
                if (moveNext != null)
                {
                    return moveNext;
                }
            }

            return null;
        }

        internal static bool IsPatched(HarmonyLib.Harmony harmony, MethodBase? expected)
        {
            if (expected == null)
            {
                return false;
            }

            HarmonyLib.Patches? info = HarmonyLib.Harmony.GetPatchInfo(expected);
            return info != null && info.Owners.Contains(harmony.Id);
        }
    }
}
