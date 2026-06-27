using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics;

internal static class StatisticsJson
{
    public static string SerializePlayer(PlayerStatisticsDocument doc)
    {
        var sb = new StringBuilder(512);
        sb.Append('{');
        AppendInt(sb, "version", doc.Version, true);
        AppendULong(sb, "steamId", doc.SteamId);
        AppendString(sb, "displayName", doc.DisplayName);
        AppendObject(sb, "global", () => SerializeGlobal(sb, doc.Global));
        if (doc.CurrentSession != null)
            AppendObject(sb, "currentSession", () => SerializeSession(sb, doc.CurrentSession));
        AppendArray(sb, "recentSessions", doc.RecentSessions, SerializeSession);
        sb.Append('}');
        return sb.ToString();
    }

    public static PlayerStatisticsDocument? DeserializePlayer(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var doc = new PlayerStatisticsDocument();
            var root = JsonObject.Parse(json);
            doc.Version = root.GetInt("version", PlayerStatisticsDocument.CurrentVersion);
            doc.SteamId = root.GetULong("steamId");
            doc.DisplayName = root.GetString("displayName") ?? "";
            if (root.TryGetObject("global", out var globalObj))
                doc.Global = DeserializeGlobal(globalObj);
            if (root.TryGetObject("currentSession", out var sessionObj))
                doc.CurrentSession = DeserializeSession(sessionObj);
            if (root.TryGetArray("recentSessions", out var arr))
            {
                foreach (var item in arr)
                    doc.RecentSessions.Add(DeserializeSession(item));
            }
            return doc;
        }
        catch
        {
            return null;
        }
    }

    public static string SerializeLeaderboard(LeaderboardDocument doc)
    {
        var sb = new StringBuilder(256);
        sb.Append('{');
        AppendInt(sb, "version", doc.Version, true);
        AppendInt(sb, "saveSlotId", doc.SaveSlotId);
        AppendString(sb, "updatedAtUtc", doc.UpdatedAtUtc.ToString("O"));
        sb.Append(",\"entries\":[");
        for (int i = 0; i < doc.Entries.Count; i++)
        {
            if (i > 0) sb.Append(',');
            SerializeLeaderboardEntry(sb, doc.Entries[i]);
        }
        sb.Append("]}");
        return sb.ToString();
    }

    public static LeaderboardDocument? DeserializeLeaderboard(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var root = JsonObject.Parse(json);
            var doc = new LeaderboardDocument
            {
                Version = root.GetInt("version", LeaderboardDocument.CurrentVersion),
                SaveSlotId = root.GetInt("saveSlotId"),
                UpdatedAtUtc = DateTime.Parse(root.GetString("updatedAtUtc") ?? DateTime.UtcNow.ToString("O"), null, DateTimeStyles.RoundtripKind),
            };
            if (root.TryGetArray("entries", out var arr))
            {
                foreach (var item in arr)
                    doc.Entries.Add(DeserializeLeaderboardEntry(item));
            }
            return doc;
        }
        catch
        {
            return null;
        }
    }

    private static void SerializeGlobal(StringBuilder sb, GlobalStats global)
    {
        sb.Append('{');
        SerializeCounters(sb, global.Counters, true);
        AppendInt(sb, "sessionsCompleted", global.SessionsCompleted);
        sb.Append('}');
    }

    private static GlobalStats DeserializeGlobal(JsonObject obj)
    {
        var global = new GlobalStats();
        global.Counters = DeserializeCounters(obj);
        global.SessionsCompleted = obj.GetInt("sessionsCompleted");
        return global;
    }

    private static void SerializeSession(StringBuilder sb, SessionStats session)
    {
        sb.Append('{');
        AppendString(sb, "sessionId", session.SessionId, true);
        AppendString(sb, "startedAtUtc", session.StartedAtUtc.ToString("O"));
        AppendString(sb, "lastConnectedAtUtc", session.LastConnectedAtUtc.ToString("O"));
        if (session.LastDisconnectedAtUtc.HasValue)
            AppendString(sb, "lastDisconnectedAtUtc", session.LastDisconnectedAtUtc.Value.ToString("O"));
        AppendInt(sb, "reconnectCount", session.ReconnectCount);
        AppendBool(sb, "isOpen", session.IsOpen);
        AppendObject(sb, "counters", () => SerializeCounters(sb, session.Counters, true));
        sb.Append('}');
    }

    private static SessionStats DeserializeSession(JsonObject obj)
    {
        var session = new SessionStats
        {
            SessionId = obj.GetString("sessionId") ?? "",
            StartedAtUtc = ParseUtc(obj.GetString("startedAtUtc")),
            LastConnectedAtUtc = ParseUtc(obj.GetString("lastConnectedAtUtc")),
            ReconnectCount = obj.GetInt("reconnectCount"),
            IsOpen = obj.GetBool("isOpen", true),
        };
        string? disconnected = obj.GetString("lastDisconnectedAtUtc");
        if (!string.IsNullOrEmpty(disconnected))
            session.LastDisconnectedAtUtc = ParseUtc(disconnected);
        if (obj.TryGetObject("counters", out var countersObj))
            session.Counters = DeserializeCounters(countersObj);
        return session;
    }

    private static void SerializeCounters(StringBuilder sb, StatCounters c, bool first)
    {
        if (!first) sb.Append(',');
        sb.Append("\"itemCarryCount\":").Append(c.ItemCarryCount);
        sb.Append(",\"damageToAlly\":").Append(c.DamageToAlly);
        sb.Append(",\"mimicEncounterCount\":").Append(c.MimicEncounterCount);
        sb.Append(",\"timeInStartingVolumeMs\":").Append(c.TimeInStartingVolumeMs);
        sb.Append(",\"currencyEarned\":").Append(c.CurrencyEarned);
        sb.Append(",\"voiceEvents\":").Append(c.VoiceEvents);
        sb.Append(",\"deaths\":").Append(c.Deaths);
        sb.Append(",\"revives\":").Append(c.Revives);
        sb.Append(",\"kills\":").Append(c.Kills);
        sb.Append(",\"cyclesCompleted\":").Append(c.CyclesCompleted);
        sb.Append(",\"totalConnectedSeconds\":").Append(c.TotalConnectedSeconds);
    }

    private static StatCounters DeserializeCounters(JsonObject obj)
    {
        return new StatCounters
        {
            ItemCarryCount = obj.GetLong("itemCarryCount"),
            DamageToAlly = obj.GetLong("damageToAlly"),
            MimicEncounterCount = obj.GetLong("mimicEncounterCount"),
            TimeInStartingVolumeMs = obj.GetLong("timeInStartingVolumeMs"),
            CurrencyEarned = obj.GetLong("currencyEarned"),
            VoiceEvents = obj.GetLong("voiceEvents"),
            Deaths = obj.GetLong("deaths"),
            Revives = obj.GetLong("revives"),
            Kills = obj.GetLong("kills"),
            CyclesCompleted = obj.GetInt("cyclesCompleted"),
            TotalConnectedSeconds = obj.GetLong("totalConnectedSeconds"),
        };
    }

    private static void SerializeLeaderboardEntry(StringBuilder sb, LeaderboardEntry e)
    {
        sb.Append('{');
        AppendULong(sb, "steamId", e.SteamId, true);
        AppendString(sb, "displayName", e.DisplayName);
        sb.Append(",\"itemCarryCount\":").Append(e.ItemCarryCount);
        sb.Append(",\"damageToAlly\":").Append(e.DamageToAlly);
        sb.Append(",\"mimicEncounterCount\":").Append(e.MimicEncounterCount);
        sb.Append(",\"timeInStartingVolumeMs\":").Append(e.TimeInStartingVolumeMs);
        sb.Append(",\"currencyEarned\":").Append(e.CurrencyEarned);
        sb.Append(",\"voiceEvents\":").Append(e.VoiceEvents);
        sb.Append(",\"deaths\":").Append(e.Deaths);
        sb.Append(",\"revives\":").Append(e.Revives);
        sb.Append(",\"kills\":").Append(e.Kills);
        sb.Append(",\"totalConnectedSeconds\":").Append(e.TotalConnectedSeconds);
        sb.Append(",\"sessionsCompleted\":").Append(e.SessionsCompleted);
        sb.Append('}');
    }

    private static LeaderboardEntry DeserializeLeaderboardEntry(JsonObject obj) => new()
    {
        SteamId = obj.GetULong("steamId"),
        DisplayName = obj.GetString("displayName") ?? "",
        ItemCarryCount = obj.GetLong("itemCarryCount"),
        DamageToAlly = obj.GetLong("damageToAlly"),
        MimicEncounterCount = obj.GetLong("mimicEncounterCount"),
        TimeInStartingVolumeMs = obj.GetLong("timeInStartingVolumeMs"),
        CurrencyEarned = obj.GetLong("currencyEarned"),
        VoiceEvents = obj.GetLong("voiceEvents"),
        Deaths = obj.GetLong("deaths"),
        Revives = obj.GetLong("revives"),
        Kills = obj.GetLong("kills"),
        TotalConnectedSeconds = obj.GetLong("totalConnectedSeconds"),
        SessionsCompleted = obj.GetInt("sessionsCompleted"),
    };

    private static void AppendObject(StringBuilder sb, string key, Action writeBody)
    {
        sb.Append('"').Append(key).Append("\":");
        writeBody();
    }

    private static void AppendArray(StringBuilder sb, string key, List<SessionStats> items, Action<StringBuilder, SessionStats> writeItem)
    {
        sb.Append('"').Append(key).Append("\":[");
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(',');
            writeItem(sb, items[i]);
        }
        sb.Append(']');
    }

    private static void AppendInt(StringBuilder sb, string key, int value, bool first = false)
    {
        if (!first) sb.Append(',');
        sb.Append('"').Append(key).Append("\":").Append(value);
    }

    private static void AppendULong(StringBuilder sb, string key, ulong value, bool first = false)
    {
        if (!first) sb.Append(',');
        sb.Append('"').Append(key).Append("\":").Append(value);
    }

    private static void AppendString(StringBuilder sb, string key, string? value, bool first = false)
    {
        if (!first) sb.Append(',');
        sb.Append('"').Append(key).Append("\":\"").Append(Escape(value ?? "")).Append('"');
    }

    private static void AppendBool(StringBuilder sb, string key, bool value)
    {
        sb.Append(",\"").Append(key).Append("\":").Append(value ? "true" : "false");
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

    private static DateTime ParseUtc(string? value)
    {
        if (string.IsNullOrEmpty(value)) return DateTime.UtcNow;
        return DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
    }

    private sealed class JsonObject
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
        private readonly Dictionary<string, JsonObject> _objects = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<JsonObject>> _arrays = new(StringComparer.Ordinal);

        public static JsonObject Parse(string json)
        {
            var parser = new Parser(json.Trim());
            return parser.ParseObject();
        }

        public int GetInt(string key, int defaultValue = 0) =>
            _values.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? i : defaultValue;

        public long GetLong(string key) =>
            _values.TryGetValue(key, out var v) && long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out long i) ? i : 0;

        public ulong GetULong(string key) =>
            _values.TryGetValue(key, out var v) && ulong.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong i) ? i : 0;

        public bool GetBool(string key, bool defaultValue = false) =>
            _values.TryGetValue(key, out var v) ? v == "true" : defaultValue;

        public string? GetString(string key) =>
            _values.TryGetValue(key, out var v) ? Unescape(v) : null;

        public bool TryGetObject(string key, out JsonObject obj) => _objects.TryGetValue(key, out obj!);
        public bool TryGetArray(string key, out List<JsonObject> arr) => _arrays.TryGetValue(key, out arr!);

        private static string Unescape(string s) =>
            s.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "\r");

        private sealed class Parser
        {
            private readonly string _json;
            private int _pos;

            public Parser(string json) => _json = json;

            public JsonObject ParseObject()
            {
                var obj = new JsonObject();
                Expect('{');
                SkipWhitespace();
                if (TryConsume('}')) return obj;
                while (true)
                {
                    string key = ReadString();
                    SkipWhitespace();
                    Expect(':');
                    SkipWhitespace();
                    if (Peek() == '{')
                    {
                        obj._objects[key] = ParseObject();
                    }
                    else if (Peek() == '[')
                    {
                        obj._arrays[key] = ParseArray();
                    }
                    else
                    {
                        obj._values[key] = ReadPrimitive();
                    }
                    SkipWhitespace();
                    if (TryConsume('}')) break;
                    Expect(',');
                    SkipWhitespace();
                }
                return obj;
            }

            private List<JsonObject> ParseArray()
            {
                var list = new List<JsonObject>();
                Expect('[');
                SkipWhitespace();
                if (TryConsume(']')) return list;
                while (true)
                {
                    list.Add(ParseObject());
                    SkipWhitespace();
                    if (TryConsume(']')) break;
                    Expect(',');
                    SkipWhitespace();
                }
                return list;
            }

            private string ReadString()
            {
                Expect('"');
                var sb = new StringBuilder();
                while (_pos < _json.Length)
                {
                    char c = _json[_pos++];
                    if (c == '"') break;
                    if (c == '\\' && _pos < _json.Length)
                    {
                        char esc = _json[_pos++];
                        sb.Append(c).Append(esc);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }

            private string ReadPrimitive()
            {
                int start = _pos;
                while (_pos < _json.Length && ",}]".IndexOf(_json[_pos]) < 0)
                    _pos++;
                return _json.Substring(start, _pos - start).Trim();
            }

            private char Peek() => _pos < _json.Length ? _json[_pos] : '\0';
            private void Expect(char c)
            {
                if (_pos >= _json.Length || _json[_pos] != c)
                    throw new FormatException($"Expected '{c}' at {_pos}");
                _pos++;
            }

            private bool TryConsume(char c)
            {
                if (_pos < _json.Length && _json[_pos] == c)
                {
                    _pos++;
                    return true;
                }
                return false;
            }

            private void SkipWhitespace()
            {
                while (_pos < _json.Length && char.IsWhiteSpace(_json[_pos]))
                    _pos++;
            }
        }
    }
}
