using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.JoinAnytime;

public static class JoinAnytimePatches
{
    private const string Feature = "JoinAnytime";

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        int applied = 0;
        int failed = 0;
        var patchNamespace = typeof(JoinAnytimePatches).Namespace + ".Patches";
        foreach (var type in typeof(JoinAnytimePatches).Assembly.GetTypes())
        {
            if (type.Namespace != patchNamespace)
                continue;

            if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
                continue;

            try
            {
                var processor = harmony.CreateClassProcessor(type);
                var results = processor.Patch();
                if (results != null && results.Count > 0)
                    applied += results.Count;
                else if (type.GetMethod("TargetMethod", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) != null)
                    ModLog.Warn(Feature, $"Patch {type.Name} — TargetMethod returned no patches");
            }
            catch (System.Exception ex)
            {
                failed++;
                ModLog.Warn(Feature, $"Patch {type.Name} failed — {ex.Message}");
            }
        }

        ModLog.Info(Feature, $"Patches applied — {applied} patch(es), {failed} failure(s).");
    }
}
