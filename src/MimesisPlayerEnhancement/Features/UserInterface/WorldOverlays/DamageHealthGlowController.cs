using System.Linq;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal sealed class DamageHealthGlowController
    {
        private static readonly Color HealthGreen = new(0.25f, 1.25f, 0.3f, 1f);
        private static readonly Color HealthRed = new(1.25f, 0.22f, 0.12f, 1f);
        private static readonly Color KillBloodColor = new(1.45f, 0.03f, 0.02f, 1f);

        private const float HoldDurationSeconds = 1f;
        private const float FadeOutDurationSeconds = 0.25f;

        private readonly Dictionary<int, ActiveGlow> _active = new();

        internal bool HasActiveDamageGlows => _active.Count > 0;

        internal void NotifyDamaged(ProtoActor actor, long hp, long maxHp)
        {
            if (!CanApply(actor) || maxHp <= 0)
            {
                return;
            }

            ApplyGlow(actor, hp, maxHp);
        }

        internal void NotifyDamagedFromHit(ProtoActor actor, long damage)
        {
            if (!CanApply(actor) || damage <= 0 || !TryResolveHpAfterHit(actor, damage, out long hp, out long maxHp))
            {
                return;
            }

            ApplyGlow(actor, hp, maxHp);
        }

        internal void NotifyKilled(ProtoActor actor)
        {
            if (!CanApply(actor))
            {
                return;
            }

            long maxHp = actor.netSyncActorData?.maxHP ?? 0L;
            if (maxHp <= 0
                && actor.ActorType == ActorType.Monster
                && actor.monsterMasterID > 0)
            {
                maxHp = HubGameDataAccess.Excel?.GetMonsterInfo(actor.monsterMasterID)?.HP ?? 0L;
            }

            ApplyGlow(actor, 0, Math.Max(maxHp, 1L));
        }

        internal void ReleaseForDespawn(ProtoActor? actor)
        {
            if (actor == null)
            {
                return;
            }

            Release(actor.ActorID, stopPaint: true);
        }

        internal void Tick()
        {
            if (_active.Count == 0)
            {
                return;
            }

            float now = Time.time;
            foreach (int actorId in _active.Keys.ToList())
            {
                if (!_active.TryGetValue(actorId, out ActiveGlow? glow))
                {
                    continue;
                }

                ProtoActor? actor = glow.Actor;
                if (!IsAlive(actor))
                {
                    Release(actorId, stopPaint: false);
                    continue;
                }

                if (!glow.FadeStarted && now >= glow.HoldUntil)
                {
                    glow.FadeStarted = TryBeginFade(actor!);
                    if (!glow.FadeStarted)
                    {
                        Release(actorId, stopPaint: false);
                    }

                    continue;
                }

                if (glow.FadeStarted && now >= glow.ExpiresAt)
                {
                    Release(actorId, stopPaint: false);
                }
            }
        }

        internal void TearDown()
        {
            foreach (int actorId in _active.Keys.ToList())
            {
                Release(actorId, stopPaint: true);
            }

            _active.Clear();
            WorldOverlayVisibility.ClearCache();
        }

        private static bool CanApply(ProtoActor actor) =>
            WorldOverlayGate.DamageHealthGlowEnabled
            && WorldOverlayGate.IsWorldDamageTarget(actor)
            && WorldOverlayVisibility.CanShow(actor);

        private void ApplyGlow(ProtoActor actor, long hp, long maxHp)
        {
            if (!IsAlive(actor))
            {
                return;
            }

            Color color = hp <= 0
                ? KillBloodColor
                : Color.Lerp(HealthRed, HealthGreen, Mathf.Clamp01((float)hp / maxHp));

            if (!TryStartPaint(actor, color))
            {
                return;
            }

            float now = Time.time;
            _active[actor.ActorID] = new ActiveGlow
            {
                Actor = actor,
                ActorId = actor.ActorID,
                HoldUntil = now + HoldDurationSeconds,
                ExpiresAt = now + HoldDurationSeconds + FadeOutDurationSeconds,
                FadeStarted = false,
            };
        }

        private static bool TryStartPaint(ProtoActor actor, Color color)
        {
            if (!IsAlive(actor))
            {
                return false;
            }

            try
            {
                actor.TurnOnMaterialPaint(color);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryBeginFade(ProtoActor actor)
        {
            if (!IsAlive(actor))
            {
                return false;
            }

            try
            {
                actor.TurnOffMaterialPaint(FadeOutDurationSeconds);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Release(int actorId, bool stopPaint)
        {
            if (!_active.TryGetValue(actorId, out ActiveGlow? glow))
            {
                return;
            }

            _active.Remove(actorId);
            WorldOverlayVisibility.RemoveFromCache(actorId);

            if (!stopPaint || !IsAlive(glow.Actor))
            {
                return;
            }

            TryStopPaint(glow.Actor!);
        }

        private static void TryStopPaint(ProtoActor actor)
        {
            if (!IsAlive(actor))
            {
                return;
            }

            try
            {
                actor.TurnOffMaterialPaint(0f);
            }
            catch
            {
                // Actor or renderers were destroyed mid-call.
            }
        }

        private static bool IsAlive(ProtoActor? actor) => actor != null;

        private static bool TryResolveHpAfterHit(ProtoActor actor, long damage, out long hp, out long maxHp)
        {
            hp = 0;
            maxHp = actor.netSyncActorData?.maxHP ?? 0L;
            if (maxHp <= 0
                && actor.ActorType == ActorType.Monster
                && actor.monsterMasterID > 0)
            {
                maxHp = HubGameDataAccess.Excel?.GetMonsterInfo(actor.monsterMasterID)?.HP ?? 0L;
            }

            if (maxHp <= 0)
            {
                return false;
            }

            long syncedHp = Math.Clamp(actor.netSyncActorData?.hp ?? maxHp, 0L, maxHp);
            long estimatedHp = Math.Max(0L, Math.Min(maxHp, syncedHp + damage) - damage);
            hp = syncedHp < estimatedHp ? syncedHp : estimatedHp;
            return true;
        }

        private sealed class ActiveGlow
        {
            internal ProtoActor? Actor;
            internal int ActorId;
            internal float HoldUntil;
            internal float ExpiresAt;
            internal bool FadeStarted;
        }
    }
}
