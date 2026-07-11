using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    /// <summary>
    /// Client-side HP estimates for actors whose mutable HP is not replicated (e.g. mimics/monsters).
    /// Authoritative <see cref="ProtoActor.UpdateHp"/> values take precedence when present.
    /// </summary>
    internal static class WorldOverlayHpTracker
    {
        private static readonly Dictionary<int, TrackedHp> ByActorId = new();

        internal static void ApplyDamage(ProtoActor actor, long damage)
        {
            if (damage <= 0)
            {
                return;
            }

            TrackedHp tracked = GetOrCreate(actor);
            tracked.Hp = Math.Max(0L, tracked.Hp - damage);
        }

        internal static void NotifySynced(ProtoActor actor)
        {
            TrackedHp tracked = GetOrCreate(actor);
            long netMax = actor.netSyncActorData.maxHP;
            long netHp = actor.netSyncActorData.hp;

            long resolvedMax = ResolveMaxHp(actor, netMax);
            if (resolvedMax > 0)
            {
                tracked.MaxHp = resolvedMax;
            }

            if (netMax > 0 || tracked.HasAuthoritativeHp)
            {
                tracked.Hp = Math.Clamp(netHp, 0L, tracked.MaxHp > 0 ? tracked.MaxHp : netHp);
                tracked.HasAuthoritativeHp = true;
            }
        }

        internal static bool TryGetDisplay(ProtoActor actor, out long hp, out long maxHp)
        {
            TrackedHp tracked = GetOrCreate(actor);
            hp = tracked.Hp;
            maxHp = tracked.MaxHp;
            return maxHp > 0;
        }

        internal static void Remove(int actorId)
        {
            ByActorId.Remove(actorId);
        }

        internal static void Clear()
        {
            ByActorId.Clear();
        }

        private static TrackedHp GetOrCreate(ProtoActor actor)
        {
            int actorId = actor.ActorID;
            if (!ByActorId.TryGetValue(actorId, out TrackedHp? tracked))
            {
                tracked = CreateInitial(actor);
                ByActorId[actorId] = tracked;
            }

            return tracked;
        }

        private static TrackedHp CreateInitial(ProtoActor actor)
        {
            long netMax = actor.netSyncActorData.maxHP;
            long netHp = actor.netSyncActorData.hp;
            long resolvedMax = ResolveMaxHp(actor, netMax);

            TrackedHp tracked = new();
            if (resolvedMax <= 0)
            {
                tracked.MaxHp = 0;
                tracked.Hp = netHp;
                return tracked;
            }

            tracked.MaxHp = resolvedMax;
            if (netMax > 0)
            {
                tracked.Hp = Math.Clamp(netHp, 0L, resolvedMax);
                tracked.HasAuthoritativeHp = true;
                return tracked;
            }

            // Monsters/mimics often keep default net HP on clients — assume full until damage is observed.
            tracked.Hp = resolvedMax;
            return tracked;
        }

        private static long ResolveMaxHp(ProtoActor actor, long netMax)
        {
            if (netMax > 0)
            {
                return netMax;
            }

            if (actor.ActorType != ActorType.Monster || actor.monsterMasterID <= 0)
            {
                return 0;
            }

            MonsterInfo? info = HubGameDataAccess.Excel?.GetMonsterInfo(actor.monsterMasterID);
            return info != null && info.HP > 0 ? info.HP : 0;
        }

        private sealed class TrackedHp
        {
            internal long Hp;
            internal long MaxHp;
            internal bool HasAuthoritativeHp;
        }
    }
}
