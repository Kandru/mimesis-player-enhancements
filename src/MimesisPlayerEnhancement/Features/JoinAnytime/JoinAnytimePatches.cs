using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.JoinAnytime;

public static class JoinAnytimePatches
{
    private const string Feature = "JoinAnytime";

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        var patchNamespace = typeof(JoinAnytimePatches).Namespace + ".Patches";
        foreach (var type in typeof(JoinAnytimePatches).Assembly.GetTypes())
        {
            if (type.Namespace != patchNamespace)
                continue;

            if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
                continue;

            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (System.Exception ex)
            {
                ModLog.Warn(Feature, $"Patch {type.Name} failed: {ex.Message}");
            }
        }

        ModLog.Info(Feature, "Patches applied.");
    }
}
