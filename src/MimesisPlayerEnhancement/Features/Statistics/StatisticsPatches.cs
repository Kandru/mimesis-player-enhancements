using System.Linq;
using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.Statistics;

public static class StatisticsPatches
{
    private const string Feature = "Statistics";

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        var patchNamespace = typeof(StatisticsPatches).Namespace + ".Patches";
        var patchTypes = typeof(StatisticsPatches).Assembly
            .GetTypes()
            .Where(t => t.Namespace == patchNamespace && t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);

        foreach (var type in patchTypes)
            harmony.CreateClassProcessor(type).Patch();

        ModLog.Info(Feature, "Patches applied.");
    }
}
