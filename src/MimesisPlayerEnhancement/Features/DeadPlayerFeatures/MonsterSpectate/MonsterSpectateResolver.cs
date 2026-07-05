using System;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using Mimic.Actors;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.MonsterSpectate
{
    internal static class MonsterSpectateResolver
    {
        private static bool _cachedMasterEnable;
        private static bool _cachedMonsterSpectateEnable;
        private static bool _cachedSpectateAfterPlayers;
        private static bool _cachedIncludeMimics;

        static MonsterSpectateResolver()
        {
            ModConfig.Changed += OnConfigChanged;
        }

        internal static bool IsEnabled => _cachedMasterEnable && _cachedMonsterSpectateEnable;

        internal static void RefreshFromConfigRegistration()
        {
            _cachedMasterEnable = ModConfig.EnableDeadPlayerFeatures.Value;
            _cachedMonsterSpectateEnable = ModConfig.EnableMonsterSpectate.Value;
            _cachedSpectateAfterPlayers = ModConfig.SpectateMonstersAfterPlayers.Value;
            _cachedIncludeMimics = ModConfig.IncludeMimicsInMonsterSpectate.Value;
        }

        internal static bool IsSupportedScene(GameMainBase? main) => main is GamePlayScene;

        internal static void AppendMonsters(List<ProtoActor> targets)
        {
            if (!IsEnabled || targets == null)
            {
                return;
            }

            GameMainBase? main = Hub.Main;
            if (!IsSupportedScene(main))
            {
                return;
            }

            List<ProtoActor> monsters = CollectAliveMonsters(main!);
            if (monsters.Count == 0)
            {
                return;
            }

            HashSet<int> existingIds = new(targets.Count + monsters.Count);
            foreach (ProtoActor target in targets)
            {
                if (target != null)
                {
                    existingIds.Add(target.ActorID);
                }
            }

            List<ProtoActor> toAdd = [];
            foreach (ProtoActor monster in monsters)
            {
                if (monster != null && !existingIds.Contains(monster.ActorID))
                {
                    toAdd.Add(monster);
                    existingIds.Add(monster.ActorID);
                }
            }

            if (toAdd.Count == 0)
            {
                return;
            }

            if (_cachedSpectateAfterPlayers)
            {
                toAdd.Sort(CompareActorId);
                targets.AddRange(toAdd);
                return;
            }

            targets.AddRange(toAdd);
            targets.Sort(CompareActorId);
        }

        internal static bool TryResolveTargetByName(string actorName, out ProtoActor? target)
        {
            target = null;
            if (!IsEnabled || string.IsNullOrEmpty(actorName))
            {
                return false;
            }

            GameMainBase? main = Hub.Main;
            if (!IsSupportedScene(main))
            {
                return false;
            }

            List<ProtoActor>? players = main!.GetAllPlayers();
            if (players != null)
            {
                target = players.Find(actor => actor != null && actor.nickName == actorName);
                if (target != null)
                {
                    return true;
                }
            }

            foreach (ProtoActor monster in CollectAliveMonsters(main))
            {
                if (string.Equals(GetMonsterDisplayName(monster), actorName, StringComparison.Ordinal))
                {
                    target = monster;
                    return true;
                }
            }

            return false;
        }

        internal static string GetMonsterDisplayName(ProtoActor actor)
        {
            if (actor == null)
            {
                return string.Empty;
            }

            string? monsterName = HubGameDataAccess.Excel?.GetMonsterInfo(actor.monsterMasterID)?.Name;
            if (!string.IsNullOrWhiteSpace(monsterName))
            {
                return monsterName;
            }

            return actor.netSyncActorData?.actorName ?? string.Empty;
        }

        internal static bool ShouldHandleMonsterTeleportCulling(ProtoActor teleportedActor)
        {
            CameraManager? cameraman = DeadPlayerPhoneGameAccess.TryGetCameraManager();
            if (!IsEnabled
                || teleportedActor == null
                || teleportedActor.ActorType != ActorType.Monster
                || teleportedActor.Possession.State == ProtoActor.PossessionState.Possessed
                || cameraman == null
                || !cameraman.IsSpectatorMode)
            {
                return false;
            }

            return cameraman.TryGetCurrentSpectatorTarget(out ProtoActor? target)
                && target == teleportedActor;
        }

        private static List<ProtoActor> CollectAliveMonsters(GameMainBase main)
        {
            List<ProtoActor> monsters = [];
            Dictionary<int, ProtoActor> actorMap = main.GetProtoActorMap();
            foreach (ProtoActor actor in actorMap.Values)
            {
                if (actor == null
                    || actor.dead
                    || actor.ActorType != ActorType.Monster)
                {
                    continue;
                }

                if (!_cachedIncludeMimics && actor.IsMimic())
                {
                    continue;
                }

                if (SpawnCategoryLookup.GetCategory(actor.monsterMasterID) == SpawnCategory.Trap)
                {
                    continue;
                }

                monsters.Add(actor);
            }

            monsters.Sort(CompareActorId);
            return monsters;
        }

        private static int CompareActorId(ProtoActor lhs, ProtoActor rhs) =>
            lhs.ActorID.CompareTo(rhs.ActorID);

        private static void OnConfigChanged(ModConfigChangeInfo change)
        {
            if (change.IsFullReload
                || change.AffectsSection("MimesisPlayerEnhancement_DeadPlayerFeatures"))
            {
                RefreshFromConfigRegistration();
            }
        }
    }
}
