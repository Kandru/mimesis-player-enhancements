using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Statistics;

public static class InGameMessageHelper
{
    private const BindingFlags InstanceMemberFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void ShowJoin(string displayName) =>
        ShowPlayerInfo($"{displayName} connected", isEntering: true);

    public static void ShowLeave(string displayName) =>
        ShowPlayerInfo($"{displayName} disconnected", isEntering: false);

    public static void ShowCycleSaved(int cycleNumber) =>
        ShowPlayerInfo($"[Stats] Cycle {cycleNumber} recorded", isEntering: true);

    private static void ShowPlayerInfo(string message, bool isEntering)
    {
        if (!ModConfig.ShowStatisticsToasts.Value)
            return;

        try
        {
            object? ui = GetPlayerEnterInfoUi();
            if (ui != null)
            {
                var method = ui.GetType().GetMethod("AddPlayerInfo", InstanceMemberFlags);
                method?.Invoke(ui, new object[] { message, isEntering });
                return;
            }

            ShowScreenTextFallback(message);
        }
        catch (System.Exception ex)
        {
            ModLog.Debug("Statistics", $"Toast failed: {ex.Message}");
        }
    }

    private static object? GetPlayerEnterInfoUi()
    {
        if (Hub.s == null) return null;

        object? pdata = typeof(Hub).GetField("pdata", InstanceMemberFlags)?.GetValue(Hub.s);
        if (pdata == null) return null;

        object? main = pdata.GetType().GetField("main", InstanceMemberFlags)?.GetValue(pdata);
        if (main == null) return null;

        return main.GetType().GetField("playerEnterInfoUI", InstanceMemberFlags)?.GetValue(main);
    }

    private static void ShowScreenTextFallback(string message)
    {
        var screenTextType = System.Type.GetType("DunGen.Demo.ScreenText, Assembly-CSharp");
        if (screenTextType == null) return;

        var findMethod = typeof(UnityEngine.Object).GetMethod(
            "FindFirstObjectByType",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(System.Type) },
            null)
            ?? typeof(UnityEngine.Object).GetMethod(
                "FindObjectOfType",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(System.Type) },
                null);
        if (findMethod == null) return;

        object? instance = findMethod.Invoke(null, new object[] { screenTextType });
        if (instance == null) return;

        var addMessage = screenTextType.GetMethod("AddMessage", InstanceMemberFlags);
        addMessage?.Invoke(instance, new object[] { message });
    }
}
