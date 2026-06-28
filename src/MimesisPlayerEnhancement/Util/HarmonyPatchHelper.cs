using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader.Logging;

namespace MimesisPlayerEnhancement.Util;

public static class HarmonyPatchHelper
{
    public struct PatchApplyResult
    {
        public int Applied;
        public int Failed;
    }

    public static IEnumerable<Type> GetNestedPatchTypes(Type containerType) =>
        containerType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);

    public static IEnumerable<Type> GetNamespacePatchTypes(Type anchorType, string suffix = ".Patches") =>
        anchorType.Assembly.GetTypes()
            .Where(t => t.Namespace == anchorType.Namespace + suffix
                        && t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);

    public static PatchApplyResult ApplyPatchTypes(HarmonyLib.Harmony harmony, string feature, IEnumerable<Type> patchTypes)
    {
        int applied = 0;
        int failed = 0;

        foreach (var patchType in patchTypes)
        {
            try
            {
                var results = harmony.CreateClassProcessor(patchType).Patch();
                if (results != null && results.Count > 0)
                {
                    applied += results.Count;
                }
                else
                {
                    failed++;
                    if (patchType.GetMethod("TargetMethod", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null)
                        ModLog.Warn(feature, $"Patch {patchType.Name} — TargetMethod returned no patches");
                    else
                        ModLog.Warn(feature, $"Patch class {patchType.Name} did not apply to any methods.");
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
            return;

        var patched = harmony.GetPatchedMethods().ToList();
        var entries = new List<(string text, bool ok)>();

        foreach (var (label, method) in checks)
        {
            string text = method == null ? $"{label} (type/method not found)" : label;
            bool ok = method != null && IsPatched(patched, method);
            entries.Add((text, ok));
        }

        if (entries.Count == 0)
            return;

        string stripped = string.Join(", ", entries.Select(e => e.text));

        var segments = new (ColorARGB? color, string text)[entries.Count * 2 - 1];
        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0)
                segments[i * 2 - 1] = (null, ", ");

            var (text, ok) = entries[i];
            segments[i * 2] = (ok ? ModLog.SuccessGreen : ModLog.FailureRed, text);
        }

        ModLog.PassLogSegmented(ModLog.FeatureSection(feature, "Patch Audit"), stripped, segments);
    }

    public static bool IsPatched(IReadOnlyCollection<MethodBase> patched, MethodBase? expected)
    {
        if (expected == null)
            return false;

        foreach (var candidate in patched)
        {
            if (candidate.MetadataToken == expected.MetadataToken && ReferenceEquals(candidate.Module, expected.Module))
                return true;

            if (candidate.Name != expected.Name)
                continue;

            if (candidate.DeclaringType != expected.DeclaringType)
                continue;

            var candidateParams = candidate.GetParameters();
            var expectedParams = expected.GetParameters();
            if (candidateParams.Length != expectedParams.Length)
                continue;

            bool paramsMatch = true;
            for (int i = 0; i < candidateParams.Length; i++)
            {
                if (candidateParams[i].ParameterType != expectedParams[i].ParameterType)
                {
                    paramsMatch = false;
                    break;
                }
            }

            if (paramsMatch)
                return true;
        }

        return false;
    }
}
