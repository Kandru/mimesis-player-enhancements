using System.Linq;
using System.Reflection;
using MelonLoader.Logging;

namespace MimesisPlayerEnhancement.Util
{
    public static class HarmonyPatchHelper
    {
        public struct PatchApplyResult
        {
            public int Applied;
            public int Failed;
        }

        public static IEnumerable<Type> GetNestedPatchTypes(Type containerType)
        {
            return containerType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);
        }

        public static IEnumerable<Type> GetNamespacePatchTypes(Type anchorType, string suffix = ".Patches")
        {
            return anchorType.Assembly.GetTypes()
                .Where(t => t.Namespace == anchorType.Namespace + suffix
                            && t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);
        }

        public static PatchApplyResult ApplyPatchTypes(HarmonyLib.Harmony harmony, string feature, IEnumerable<Type> patchTypes)
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

        public static void LogPatchSummary(string feature, PatchApplyResult result)
        {
            string patchCount = $"{result.Applied} patch(es)";
            string failures = $"{result.Failed} failure(s).";
            string stripped = $"{patchCount}, {failures}";

            ModLog.PassLogSegmented(
                ModLog.FeatureSection(feature, "Patches Applied"),
                stripped,
                (result.Applied > 0 ? ModLog.SuccessGreen : null, patchCount),
                (null, ", "),
                (result.Failed > 0 ? ModLog.FailureRed : null, failures));
        }

        public static void LogPatchAudit(string feature, HarmonyLib.Harmony harmony, IEnumerable<(string label, MethodBase? method)> checks)
        {
            if (!ModConfig.EnableDebugLogging.Value)
            {
                return;
            }

            List<(string text, bool ok)> entries = [];

            foreach ((string? label, MethodBase? method) in checks)
            {
                string text = method == null ? $"{label} (type/method not found)" : label;
                bool ok = IsPatched(harmony, method);
                entries.Add((text, ok));
            }

            if (entries.Count == 0)
            {
                return;
            }

            string stripped = string.Join(", ", entries.Select(e => e.text));

            (ColorARGB? color, string text)[] segments = new (ColorARGB? color, string text)[(entries.Count * 2) - 1];
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    segments[(i * 2) - 1] = (null, ", ");
                }

                (string? text, bool ok) = entries[i];
                segments[i * 2] = (ok ? ModLog.SuccessGreen : ModLog.FailureRed, text);
            }

            ModLog.PassLogSegmented(ModLog.FeatureSection(feature, "Patch Audit"), stripped, segments);
        }

        /// <summary>
        /// Returns the compiler-generated MoveNext for an iterator method (IEnumerator/async).
        /// Transpilers must patch MoveNext — not the outer iterator stub.
        /// </summary>
        public static MethodInfo? GetEnumeratorMoveNext(Type declaringType, string methodName, Type[]? parameters = null)
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

        public static bool IsPatched(HarmonyLib.Harmony harmony, MethodBase? expected)
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
