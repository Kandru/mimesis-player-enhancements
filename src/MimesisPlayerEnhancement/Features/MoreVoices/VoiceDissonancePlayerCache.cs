namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Replaces <c>FindObjectsByType&lt;FishNetDissonancePlayer&gt;</c> with archive registry lookups.
    /// Vanilla passes the result to <c>AddOrRemoveContexts</c> but never reads it — this cache stays correct if that changes.
    /// </summary>
    internal static class VoiceDissonancePlayerCache
    {
        private static readonly Dictionary<string, FishNetDissonancePlayer> _players = [];
        private static bool _dirty = true;

        internal static void MarkDirty()
        {
            _dirty = true;
        }

        internal static void Clear()
        {
            _players.Clear();
            _dirty = true;
        }

        internal static Dictionary<string, FishNetDissonancePlayer> GetPlayers()
        {
            if (!_dirty)
            {
                return _players;
            }

            _players.Clear();
            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                if (archive == null)
                {
                    continue;
                }

                try
                {
                    FishNetDissonancePlayer player = archive.Player;
                    if (player == null || string.IsNullOrEmpty(player.PlayerId))
                    {
                        continue;
                    }

                    if (!_players.ContainsKey(player.PlayerId))
                    {
                        _players[player.PlayerId] = player;
                    }
                }
                catch
                {
                    /* archive may be partially torn down */
                }
            }

            _dirty = false;
            return _players;
        }
    }
}
