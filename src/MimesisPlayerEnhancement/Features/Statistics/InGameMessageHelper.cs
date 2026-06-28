using System;
using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Statistics;

/// <summary>
/// Bottom-left in-game toasts via <see cref="UIPrefab_PlayerEnterInfo"/>.
/// Use <see cref="ShowModMessage"/> for plain English — do not call
/// <see cref="UIPrefab_PlayerEnterInfo.AddPlayerInfo"/> for mod text; it localizes via
/// <c>ROOM_ENTER_STRING</c> / <c>ROOM_EXIT_STRING</c> and the <c>[usernickname:]</c> placeholder.
/// </summary>
public static class InGameMessageHelper
{
    internal const string MessagePrefix = "[PlayerEnhancements]";

    /// <summary>Distinct from the game's green enter / red exit toasts (see <see cref="UIPrefab_PlayerEnterInfo"/>).</summary>
    private const string LocalOnlyRichTextColor = "#88CCFF";

    private const BindingFlags InstanceMemberFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <param name="localOnly">
    /// True when the toast is shown only to the local player (e.g. session intro).
    /// Wrapped in a TMP color tag so it stands out from join/leave stats everyone sees.
    /// </param>
    public static void ShowModMessage(string message, bool isEntering = true, bool localOnly = false)
    {
        if (!ModConfig.ShowStatisticsToasts.Value && !ModConfig.ShowPlayerAnnouncements.Value)
            return;

        if (string.IsNullOrWhiteSpace(message))
            return;

        string formatted = FormatModMessage(message, localOnly);

        try
        {
            if (TryEnqueueRawPlayerInfo(formatted, isEntering))
                return;

            ModLog.Debug("Statistics", "Player enter info UI unavailable for mod toast.");
        }
        catch (Exception ex)
        {
            ModLog.Debug("Statistics", $"Mod toast failed: {ex.Message}");
        }
    }

    internal static string FormatModMessage(string message, bool localOnly)
    {
        string text = $"{MessagePrefix} {message.Trim()}";
        if (localOnly)
            text = $"<color={LocalOnlyRichTextColor}>{text}</color>";

        return text;
    }

    private static bool TryEnqueueRawPlayerInfo(string message, bool isEntering)
    {
        if (GetPlayerEnterInfoUi() is not UIPrefab_PlayerEnterInfo ui)
            return false;

        IList? currentDisplayed = GetListField(ui, "currentDisplayed");
        IList? displayStartTime = GetListField(ui, "displayStartTimeMilliSec");
        IList? isEnteringFlags = GetListField(ui, "isEnteringFlags");
        IList? playerInfos = GetListField(ui, "PlayerInfos");
        if (currentDisplayed == null || displayStartTime == null || isEnteringFlags == null || playerInfos == null)
            return false;

        if (playerInfos.Count <= 0)
            return false;

        TrimOldestIfFull(currentDisplayed, displayStartTime, isEnteringFlags, playerInfos.Count);

        currentDisplayed.Add(message);
        displayStartTime.Add(GetCurrentTickMilliSec());
        isEnteringFlags.Add(isEntering);
        return true;
    }

    private static void TrimOldestIfFull(
        IList currentDisplayed,
        IList displayStartTime,
        IList isEnteringFlags,
        int maxVisible)
    {
        while (currentDisplayed.Count >= maxVisible && currentDisplayed.Count > 0)
        {
            currentDisplayed.RemoveAt(0);
            displayStartTime.RemoveAt(0);
            isEnteringFlags.RemoveAt(0);
        }
    }

    private static IList? GetListField(object target, string fieldName)
    {
        FieldInfo? field = target.GetType().GetField(fieldName, InstanceMemberFlags);
        return field?.GetValue(target) as IList;
    }

    private static long GetCurrentTickMilliSec()
    {
        try
        {
            if (Hub.s == null)
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            object? timeUtil = typeof(Hub).GetProperty("timeutil", InstanceMemberFlags)?.GetValue(Hub.s)
                               ?? typeof(Hub).GetField("timeutil", InstanceMemberFlags)?.GetValue(Hub.s);
            if (timeUtil == null)
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            MethodInfo? getTick = timeUtil.GetType().GetMethod(
                "GetCurrentTickMilliSec",
                InstanceMemberFlags,
                null,
                Type.EmptyTypes,
                null);
            if (getTick == null)
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            return Convert.ToInt64(getTick.Invoke(timeUtil, null));
        }
        catch
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    private static UIPrefab_PlayerEnterInfo? GetPlayerEnterInfoUi()
    {
        if (Hub.s == null)
            return null;

        object? pdata = typeof(Hub).GetField("pdata", InstanceMemberFlags)?.GetValue(Hub.s);
        if (pdata == null)
            return null;

        object? main = pdata.GetType().GetField("main", InstanceMemberFlags)?.GetValue(pdata);
        if (main == null)
            return null;

        return main.GetType().GetField("playerEnterInfoUI", InstanceMemberFlags)?.GetValue(main)
               as UIPrefab_PlayerEnterInfo;
    }
}
