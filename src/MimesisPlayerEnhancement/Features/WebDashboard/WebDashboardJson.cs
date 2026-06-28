using System.Collections.Generic;
using System.Globalization;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardJson
    {
        public static string SerializeStatus(WebDashboardStatusDto status)
        {
            return ModJson.Serialize(status);
        }

        public static string SerializePlayers(IReadOnlyList<WebDashboardPlayerDto> players)
        {
            return ModJson.Serialize(new PlayersApiResponse { Players = [.. players] });
        }

        public static string SerializeLeaderboardResponse(LeaderboardDocument doc, IReadOnlyCollection<ulong> connectedSteamIds)
        {
            return ModJson.Serialize(new LeaderboardApiResponse
            {
                SaveSlotId = doc.SaveSlotId,
                UpdatedAtUtc = doc.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                ConnectedSteamIds = [.. connectedSteamIds],
                Entries = doc.Entries,
            });
        }

        public static string SerializeActionResult(WebDashboardActionResult result)
        {
            return ModJson.Serialize(result);
        }

        public static string SerializeError(int statusCode, string message)
        {
            return ModJson.Serialize(new ErrorApiResponse
            {
                Error = statusCode,
                Message = message,
            });
        }

        private sealed class PlayersApiResponse
        {
            public List<WebDashboardPlayerDto> Players = [];
        }

        private sealed class LeaderboardApiResponse
        {
            public int SaveSlotId;
            public string UpdatedAtUtc = "";
            public List<ulong> ConnectedSteamIds = [];
            public List<LeaderboardEntry> Entries = [];
        }

        private sealed class ErrorApiResponse
        {
            public int Error;
            public string Message = "";
        }
    }
}
