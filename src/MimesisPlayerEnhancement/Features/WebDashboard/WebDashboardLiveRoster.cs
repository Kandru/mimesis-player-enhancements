using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal readonly struct WebDashboardLivePlayer
    {
        internal ProtoActor Actor { get; }
        internal ulong SteamId { get; }
        internal long PlayerUid { get; }

        internal WebDashboardLivePlayer(ProtoActor actor, ulong steamId, long playerUid)
        {
            Actor = actor;
            SteamId = steamId;
            PlayerUid = playerUid;
        }
    }

    /// <summary>
    /// Single source of truth for spawned in-session players on host and guest clients.
    /// </summary>
    internal readonly struct WebDashboardLiveRoster
    {
        private readonly List<WebDashboardLivePlayer> _players;
        private readonly Dictionary<long, ProtoActor> _byUid;
        private readonly Dictionary<ulong, ProtoActor> _bySteamId;

        private WebDashboardLiveRoster(
            List<WebDashboardLivePlayer> players,
            Dictionary<long, ProtoActor> byUid,
            Dictionary<ulong, ProtoActor> bySteamId)
        {
            _players = players;
            _byUid = byUid;
            _bySteamId = bySteamId;
        }

        internal static WebDashboardLiveRoster Capture()
        {
            List<WebDashboardLivePlayer> players = [];
            Dictionary<long, ProtoActor> byUid = [];
            Dictionary<ulong, ProtoActor> bySteamId = [];

            try
            {
                Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
                GameMainBase? main = pdata?.main;
                Dictionary<int, ProtoActor>? map = main?.GetProtoActorMap();
                if (map == null)
                {
                    return new WebDashboardLiveRoster(players, byUid, bySteamId);
                }

                foreach (ProtoActor? actor in map.Values)
                {
                    if (actor == null || actor.ActorType != ActorType.Player)
                    {
                        continue;
                    }

                    long playerUid = actor.UID;
                    if (playerUid == 0)
                    {
                        continue;
                    }

                    ulong steamId = StatisticsTracker.TryResolveSteamId(actor);
                    if (steamId == 0)
                    {
                        continue;
                    }

                    byUid[playerUid] = actor;
                    bySteamId[steamId] = actor;
                    players.Add(new WebDashboardLivePlayer(actor, steamId, playerUid));
                }
            }
            catch
            {
                /* scene may be transitioning */
            }

            return new WebDashboardLiveRoster(players, byUid, bySteamId);
        }

        internal IEnumerable<WebDashboardLivePlayer> Enumerate()
        {
            return _players;
        }

        internal bool TryGetByUid(long playerUid, out ProtoActor actor)
        {
            return _byUid.TryGetValue(playerUid, out actor!);
        }

        internal bool TryGetBySteamId(ulong steamId, out ProtoActor actor)
        {
            return _bySteamId.TryGetValue(steamId, out actor!);
        }

        internal string? ResolveNickName(long playerUid, ulong steamId)
        {
            if (playerUid != 0 && TryGetByUid(playerUid, out ProtoActor actor) && IsUsableNick(actor.nickName))
            {
                return actor.nickName;
            }

            if (steamId != 0 && TryGetBySteamId(steamId, out actor) && IsUsableNick(actor.nickName))
            {
                return actor.nickName;
            }

            return null;
        }

        internal bool TryGetAlive(long playerUid, ulong steamId, out bool isAlive)
        {
            isAlive = true;
            if (playerUid != 0 && TryGetByUid(playerUid, out ProtoActor actor))
            {
                isAlive = !actor.dead;
                return true;
            }

            if (steamId != 0 && TryGetBySteamId(steamId, out actor))
            {
                isAlive = !actor.dead;
                return true;
            }

            return false;
        }

        internal bool TryGetVitals(
            long playerUid,
            ulong steamId,
            out long health,
            out long maxHealth,
            out double toxicPercent)
        {
            health = 0;
            maxHealth = 0;
            toxicPercent = 0;
            ProtoActor? actor = null;
            if (playerUid != 0 && TryGetByUid(playerUid, out ProtoActor byUid))
            {
                actor = byUid;
            }
            else if (steamId != 0 && TryGetBySteamId(steamId, out ProtoActor bySteam))
            {
                actor = bySteam;
            }

            if (actor == null)
            {
                return false;
            }

            health = actor.netSyncActorData.hp;
            maxHealth = actor.netSyncActorData.maxHP;
            long conta = actor.netSyncActorData.conta;
            long maxConta = actor.netSyncActorData.maxConta;
            toxicPercent = ComputeVitalPercent(conta, maxConta) ?? 0;
            return true;
        }

        private static bool IsUsableNick(string? nickName)
        {
            return !string.IsNullOrWhiteSpace(nickName);
        }

        private static double? ComputeVitalPercent(long current, long max)
        {
            if (max <= 0)
            {
                return null;
            }

            return System.Math.Clamp((double)current / max * 100.0, 0.0, 100.0);
        }
    }
}
