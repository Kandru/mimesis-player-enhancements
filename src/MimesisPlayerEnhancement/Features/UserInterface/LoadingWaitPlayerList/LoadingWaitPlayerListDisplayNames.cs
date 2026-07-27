namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListDisplayNames
    {
        internal static string Resolve(string? nickName, ulong steamId)
        {
            if (!string.IsNullOrWhiteSpace(nickName))
            {
                return nickName;
            }

            if (PlayerRegistry.TryGetRecord(steamId, out PlayerRecord? record)
                && !string.IsNullOrWhiteSpace(record.DisplayName))
            {
                return record.DisplayName;
            }

            return string.Empty;
        }
    }
}
