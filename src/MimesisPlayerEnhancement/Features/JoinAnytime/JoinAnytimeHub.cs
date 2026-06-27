using System.Reflection;

namespace MimesisPlayerEnhancement.Features.JoinAnytime;

internal static class JoinAnytimeHub
{
    private const string Feature = "JoinAnytime";

    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo? PdataField =
        typeof(Hub).GetField("pdata", InstanceFlags);

    private static readonly FieldInfo? SteamInviteField =
        typeof(Hub).GetField("steamInviteDispatcher", InstanceFlags);

    private static bool _warnedMissingFields;

    internal static Hub.PersistentData? GetPdata()
    {
        if (Hub.s == null)
            return null;

        if (PdataField == null)
        {
            WarnMissingFieldsOnce();
            return null;
        }

        return PdataField.GetValue(Hub.s) as Hub.PersistentData;
    }

    internal static SteamInviteDispatcher? GetSteamInviteDispatcher()
    {
        if (Hub.s == null)
            return null;

        if (SteamInviteField == null)
        {
            WarnMissingFieldsOnce();
            return null;
        }

        return SteamInviteField.GetValue(Hub.s) as SteamInviteDispatcher;
    }

    private static void WarnMissingFieldsOnce()
    {
        if (_warnedMissingFields)
            return;

        _warnedMissingFields = true;
        ModLog.Warn(Feature, "Hub reflection fields missing — pdata or steamInviteDispatcher not found");
    }
}
