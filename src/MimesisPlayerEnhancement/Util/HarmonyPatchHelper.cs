using System.Linq;
using System.Reflection;
using MelonLoader.Logging;

namespace MimesisPlayerEnhancement.Util
{
    internal static class HarmonyPatchHelper
    {
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

        internal static void ApplyPatchTypes(HarmonyLib.Harmony harmony, string feature, IEnumerable<Type> patchTypes)
        {
            int applied = 0;
            int failed = 0;
            Dictionary<string, CategoryAudit> audits = new(StringComparer.Ordinal);

            foreach (Type patchType in patchTypes)
            {
                string category = ResolvePatchCategory(patchType, feature);
                CategoryAudit audit = GetOrCreateCategory(audits, category);

                try
                {
                    List<MethodInfo> results = harmony.CreateClassProcessor(patchType).Patch();
                    if (results != null && results.Count > 0)
                    {
                        applied += results.Count;
                        foreach (MethodInfo method in results)
                        {
                            audit.AddOk(FormatMethodLabel(method));
                        }
                    }
                    else
                    {
                        failed++;
                        audit.AddMiss(patchType.Name);
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
                    audit.AddMiss(patchType.Name);
                    ModLog.Warn(feature, $"Patch {patchType.Name} failed — {ex.Message}");
                }
            }

            LogPatchAudits(feature, audits);
            LogPatchSummary(feature, applied, failed);
        }

        private static void LogPatchSummary(string feature, int applied, int failed)
        {
            string message = $"{applied} patch(es), {failed} failure(s).";
            ColorARGB? lineColor = PickOutcomeLineColor(applied, failed);

            ModLog.PassLogSegmented(ModLog.FeatureSection(feature, "Patches Applied"), message, (lineColor, message));
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

        private static void LogPatchAudits(string feature, Dictionary<string, CategoryAudit> audits)
        {
            if (!ModConfig.EnableDebugLogging.Value || audits.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, CategoryAudit> pair in audits.OrderBy(static e => e.Key, StringComparer.Ordinal))
            {
                List<(string text, bool ok)> entries = pair.Value.ToEntries();
                if (entries.Count == 0)
                {
                    continue;
                }

                int missed = entries.Count(static e => !e.ok);
                string message = string.Join(", ", entries.Select(static e => e.text));
                ColorARGB? lineColor = PickOutcomeLineColor(entries.Count - missed, missed);
                string title = string.Equals(pair.Key, feature, StringComparison.Ordinal)
                    ? "Patch Audit"
                    : $"Patch Audit/{pair.Key}";

                ModLog.PassLogSegmented(ModLog.FeatureSection(feature, title), message, (lineColor, message));
            }
        }

        private static CategoryAudit GetOrCreateCategory(Dictionary<string, CategoryAudit> audits, string category)
        {
            if (!audits.TryGetValue(category, out CategoryAudit? audit))
            {
                audit = new CategoryAudit();
                audits[category] = audit;
            }

            return audit;
        }

        private static string ResolvePatchCategory(Type patchType, string feature)
        {
            if (patchType.DeclaringType is Type owner)
            {
                string fromOwner = TrimPatchesSuffix(owner.Name);
                if (fromOwner.Length > 0)
                {
                    return fromOwner;
                }
            }

            if (patchType.Namespace is string ns
                && ns.EndsWith(".Patches", StringComparison.Ordinal)
                && ns.Length > ".Patches".Length)
            {
                string parent = ns[..^".Patches".Length];
                int dot = parent.LastIndexOf('.');
                string segment = dot >= 0 ? parent[(dot + 1)..] : parent;
                // Shared infra folders aren't useful subcategories — keep the feature name.
                if (segment.Length > 0
                    && !string.Equals(segment, "Config", StringComparison.Ordinal)
                    && !string.Equals(segment, "Util", StringComparison.Ordinal))
                {
                    return segment;
                }
            }

            return feature;
        }

        private static string TrimPatchesSuffix(string name)
        {
            if (name.EndsWith("Patches", StringComparison.Ordinal))
            {
                return name[..^"Patches".Length];
            }

            if (name.EndsWith("Patch", StringComparison.Ordinal))
            {
                return name[..^"Patch".Length];
            }

            return name;
        }

        private static string FormatMethodLabel(MethodBase method)
        {
            // HarmonyX/MonoMod often returns DynamicMethod wrappers named DMD<Type::Method>
            // with a null DeclaringType — parse those back to Method/Type labels.
            if (TryParseHarmonyDmdName(method.Name, out string dmdType, out string dmdMethod))
            {
                return $"{dmdMethod}/{dmdType}";
            }

            string typeName = method.DeclaringType?.Name ?? "?";
            string methodName = method.IsConstructor ? ".ctor" : method.Name;
            return $"{methodName}/{typeName}";
        }

        private static bool TryParseHarmonyDmdName(string name, out string typeName, out string methodName)
        {
            typeName = "?";
            methodName = name;

            if (!name.StartsWith("DMD<", StringComparison.Ordinal)
                || !name.EndsWith(">", StringComparison.Ordinal)
                || name.Length < 6)
            {
                return false;
            }

            string inner = name[4..^1];
            int sep = inner.LastIndexOf("::", StringComparison.Ordinal);
            if (sep <= 0 || sep + 2 >= inner.Length)
            {
                return false;
            }

            string fullType = inner[..sep];
            methodName = inner[(sep + 2)..];
            if (methodName.Length == 0)
            {
                return false;
            }

            int lastDot = fullType.LastIndexOf('.');
            typeName = lastDot >= 0 ? fullType[(lastDot + 1)..] : fullType;
            int nested = typeName.LastIndexOf('+');
            if (nested >= 0)
            {
                typeName = typeName[(nested + 1)..];
            }

            return typeName.Length > 0;
        }

        private static ColorARGB? PickOutcomeLineColor(int succeeded, int failed)
        {
            if (failed == 0)
            {
                return succeeded > 0 ? ModLog.SuccessGreen : null;
            }

            return succeeded > 0 ? ModLog.PartialYellow : ModLog.FailureRed;
        }

        private sealed class CategoryAudit
        {
            private readonly HashSet<string> _okLabels = new(StringComparer.Ordinal);
            private readonly List<string> _missLabels = [];

            internal void AddOk(string label) => _okLabels.Add(label);

            internal void AddMiss(string label) => _missLabels.Add(label);

            internal List<(string text, bool ok)> ToEntries()
            {
                List<(string text, bool ok)> entries = [];
                foreach (string label in _okLabels.OrderBy(static s => s, StringComparer.Ordinal))
                {
                    entries.Add(($"{label} (ok)", true));
                }

                foreach (string label in _missLabels)
                {
                    entries.Add(($"{label} (miss)", false));
                }

                return entries;
            }
        }
    }
}
