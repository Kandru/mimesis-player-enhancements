using System.Reflection;
using Mimic.Actors;
using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement;

public static class VoiceEventStats
{
    private const BindingFlags InstanceMemberFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static int GetEventCount(SpeechEventArchive? archive)
    {
        if (archive == null)
            return 0;

        try
        {
            return archive.events?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// In-game display name (Steam/session nickname), not the voice-comms UUID.
    /// Mirrors <c>UIPrefab_InGameMenu.ResolveNickName</c>.
    /// </summary>
    public static string ResolveDisplayName(SpeechEventArchive? archive, long playerUid, bool isLocal)
    {
        if (archive != null)
        {
            try
            {
                ProtoActor? proto = archive.Player?.ProtoActorCache;
                if (proto != null && !string.IsNullOrWhiteSpace(proto.nickName))
                    return proto.nickName;
            }
            catch
            {
                /* Player / voice component may not be ready */
            }
        }

        if (playerUid != 0)
        {
            string? fromMap = ResolveNickNameFromActorMap(playerUid);
            if (!string.IsNullOrWhiteSpace(fromMap))
                return fromMap;
        }

        if (isLocal)
        {
            string? hostNick = GetHostNickName();
            if (!string.IsNullOrWhiteSpace(hostNick))
                return hostNick;
        }

        return "(pending)";
    }

    /// <summary>
    /// Voice-comms identifier (Dissonance / syncedCommsPlayerName). Used internally for persistence matching.
    /// </summary>
    public static string GetVoiceId(SpeechEventArchive? archive)
    {
        if (archive == null)
            return "?";

        try
        {
            string? voiceId = archive.PlayerId;
            return string.IsNullOrEmpty(voiceId) ? "(pending)" : voiceId;
        }
        catch
        {
            return "(unavailable)";
        }
    }

    public static string DescribePlayer(SpeechEventArchive? archive)
    {
        if (archive == null)
            return "archive=null";

        long playerUid = 0;
        bool isLocal = false;

        try
        {
            playerUid = archive.PlayerUID;
            isLocal = archive.IsLocal;
        }
        catch
        {
            /* Player component may not be ready yet */
        }

        int count = GetEventCount(archive);
        string role = isLocal ? "host" : "client";
        string uid = playerUid == 0 ? "(pending)" : playerUid.ToString();
        string name = ResolveDisplayName(archive, playerUid, isLocal);
        return $"uid={uid} name={name} role={role} voiceEvents={count}";
    }

    /// <summary>Same as <see cref="DescribePlayer"/> plus voice-comms UUID (debug only).</summary>
    public static string DescribePlayerVerbose(SpeechEventArchive? archive)
    {
        string summary = DescribePlayer(archive);
        if (archive == null)
            return summary;

        return $"{summary} voiceId={GetVoiceId(archive)}";
    }

    private static string? ResolveNickNameFromActorMap(long playerUid)
    {
        try
        {
            object? main = GetGameMain();
            if (main == null)
                return null;

            var getMap = main.GetType().GetMethod("GetProtoActorMap", InstanceMemberFlags);
            if (getMap?.Invoke(main, null) is not System.Collections.Generic.Dictionary<int, ProtoActor> map)
                return null;

            foreach (ProtoActor? actor in map.Values)
            {
                if (actor == null || actor.UID != playerUid)
                    continue;

                return string.IsNullOrWhiteSpace(actor.nickName) ? null : actor.nickName;
            }
        }
        catch
        {
            /* Hub / actor map may be unavailable during teardown */
        }

        return null;
    }

    private static string? GetHostNickName()
    {
        try
        {
            object? main = GetGameMain();
            if (main != null)
            {
                var getHostNick = main.GetType().GetMethod("GetHostActorNickName", InstanceMemberFlags);
                if (getHostNick?.Invoke(main, null) is string hostNick && !string.IsNullOrWhiteSpace(hostNick))
                    return hostNick;
            }

            object? pdata = GetHubMember("pdata");
            if (pdata == null)
                return null;

            var myNickField = pdata.GetType().GetField("MyNickName", InstanceMemberFlags);
            if (myNickField?.GetValue(pdata) is string myNick && !string.IsNullOrWhiteSpace(myNick))
                return myNick;
        }
        catch
        {
            /* Hub may be unavailable */
        }

        return null;
    }

    private static object? GetGameMain()
    {
        object? pdata = GetHubMember("pdata");
        if (pdata == null)
            return null;

        var mainField = pdata.GetType().GetField("main", InstanceMemberFlags);
        return mainField?.GetValue(pdata);
    }

    private static object? GetHubMember(string name)
    {
        if (Hub.s == null)
            return null;

        var hubType = typeof(Hub);
        var field = hubType.GetField(name, InstanceMemberFlags);
        if (field != null)
            return field.GetValue(Hub.s);

        var prop = hubType.GetProperty(name, InstanceMemberFlags);
        if (prop != null && prop.CanRead)
            return prop.GetValue(Hub.s);

        return null;
    }
}
