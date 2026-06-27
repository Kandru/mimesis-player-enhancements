using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement;

public static class VoiceEventStats
{
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

    public static string DescribePlayer(SpeechEventArchive? archive)
    {
        if (archive == null)
            return "archive=null";

        string playerId = "?";
        long playerUid = 0;
        bool isLocal = false;

        try
        {
            playerId = string.IsNullOrEmpty(archive.PlayerId) ? "(pending)" : archive.PlayerId;
            playerUid = archive.PlayerUID;
            isLocal = archive.IsLocal;
        }
        catch
        {
            playerId = "(unavailable)";
        }

        int count = GetEventCount(archive);
        string role = isLocal ? "host" : "client";
        string uid = playerUid == 0 ? "(pending)" : playerUid.ToString();
        return $"uid={uid} player={playerId} role={role} voiceEvents={count}";
    }
}
