using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsDeathHandler
    {
        private static readonly Dictionary<ulong, long> AliveSinceTickMs = [];
        private static readonly HashSet<ulong> DiedThisDungeon = [];

        internal static void OnDungeonStarted()
        {
            DiedThisDungeon.Clear();
            TrainDepositTracker.ClearDungeonState();
            StampConnectedPlayers();
        }

        internal static void OnDungeonEnded(IEnumerable<VPlayer> players, bool notify = true)
        {
            bool changed = false;
            foreach (VPlayer player in players)
            {
                if (player == null || player.SteamID == 0)
                {
                    continue;
                }

                ulong steamId = player.SteamID;
                bool died = DiedThisDungeon.Contains(steamId);
                StatisticsCounterWriter.Modify(
                    steamId,
                    counters =>
                    {
                        if (died)
                        {
                            counters.DungeonExitsDead++;
                        }
                        else
                        {
                            counters.DungeonExitsAlive++;
                        }
                    },
                    notify: false);
                changed = true;
            }

            DiedThisDungeon.Clear();
            if (changed && notify)
            {
                StatisticsCounterWriter.NotifyChanged();
            }
        }

        internal static void OnPlayerRevived(ulong steamId)
        {
            if (steamId == 0)
            {
                return;
            }

            StampAlive(steamId);
        }

        internal static void HandleActorDeath(IVroom room, GameActorDeadEventArgs args)
        {
            if (room == null || args?.Victim == null)
            {
                return;
            }

            if (args.Victim is VPlayer player)
            {
                HandlePlayerDeath(player, args, room);
                return;
            }

            if (args.Victim is not VMonster monster || args.AttackerActorID == 0)
            {
                return;
            }

            VActor? attacker = TryFindActor(room, args.AttackerActorID);
            if (attacker is not VPlayer killer || killer.SteamID == 0 || monster.MasterID <= 0)
            {
                return;
            }

            string monsterKey = StatisticsEntityKeys.ForMonster(monster.MasterID);
            StatisticsCounterWriter.ModifyDictionary(killer.SteamID, counters => counters.MonsterKills, monsterKey);
        }

        internal static void OnPlayerDying(VPlayer player, ActorDyingSig sig, IVroom room)
        {
            if (player == null || player.SteamID == 0 || room == null)
            {
                return;
            }

            if (sig.attackerActorID != 0)
            {
                VActor? attacker = TryFindActor(room, sig.attackerActorID);
                if (attacker is VMonster)
                {
                    return;
                }
            }

            if (!DeathAttributionHelper.TryResolveTrapDeath(player, sig, room, out TrapType trapType))
            {
                return;
            }

            string trapKey = StatisticsEntityKeys.ForTrap(trapType);
            StatisticsCounterWriter.Modify(player.SteamID, counters =>
            {
                counters.TrapDeaths++;
                IncrementDictionaryValue(counters.DeathsByTrap, trapKey);
            });
        }

        internal static void ClearDungeonState()
        {
            DiedThisDungeon.Clear();
        }

        internal static void ClearRuntimeState()
        {
            AliveSinceTickMs.Clear();
            DiedThisDungeon.Clear();
        }

        private static void HandlePlayerDeath(VPlayer player, GameActorDeadEventArgs args, IVroom room)
        {
            ulong steamId = player.SteamID;
            _ = DiedThisDungeon.Add(steamId);

            bool isDeathmatch = room is DeathMatchRoom;
            VActor? attacker = args.AttackerActorID == 0 ? null : TryFindActor(room, args.AttackerActorID);
            ulong friendKillerSteamId = 0;
            long lifetimeMs = 0;
            if (AliveSinceTickMs.TryGetValue(steamId, out long sinceTickMs))
            {
                lifetimeMs = Math.Max(0, UtcNowMs() - sinceTickMs);
            }

            StatisticsCounterWriter.Modify(steamId, counters =>
            {
                if (isDeathmatch)
                {
                    counters.DeathmatchDeaths++;
                }
                else
                {
                    counters.SurvivalDeaths++;
                }

                if (lifetimeMs > 0)
                {
                    if (counters.LifetimesOnDeathMs.Count >= StatCounters.MaxLifetimeSamples)
                    {
                        counters.LifetimesOnDeathMs.RemoveAt(0);
                    }

                    counters.LifetimesOnDeathMs.Add(lifetimeMs);
                }

                if (attacker is VMonster killingMonster && killingMonster.MasterID > 0)
                {
                    IncrementDictionaryValue(
                        counters.DeathsByMonster,
                        StatisticsEntityKeys.ForMonster(killingMonster.MasterID));
                    return;
                }

                if (!isDeathmatch
                    && attacker is VPlayer friendKiller
                    && friendKiller.SteamID != steamId)
                {
                    friendKillerSteamId = friendKiller.SteamID;
                    counters.KilledByPlayers++;
                    IncrementDictionaryValue(counters.DeathsByMonster, StatisticsEntityKeys.PlayerKey);
                }
            }, notify: false);

            if (friendKillerSteamId != 0)
            {
                StatisticsCounterWriter.Modify(
                    friendKillerSteamId,
                    counters => counters.FriendsKilled++,
                    notify: false);
            }

            StatisticsCounterWriter.NotifyChanged();
        }

        private static void IncrementDictionaryValue(Dictionary<string, long> dictionary, string key)
        {
            _ = dictionary.TryGetValue(key, out long current);
            dictionary[key] = current + 1;
        }

        private static void StampConnectedPlayers()
        {
            long now = UtcNowMs();
            foreach (ulong steamId in PlayerRegistry.GetConnectedSteamIds())
            {
                StampAlive(steamId, now);
            }
        }

        private static void StampAlive(ulong steamId, long? tickMs = null)
        {
            if (steamId == 0)
            {
                return;
            }

            AliveSinceTickMs[steamId] = tickMs ?? UtcNowMs();
        }

        private static long UtcNowMs() => DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

        private static VActor? TryFindActor(IVroom room, int actorId)
        {
            try
            {
                return room.FindActorByObjectID(actorId);
            }
            catch
            {
                return null;
            }
        }
    }
}
