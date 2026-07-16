using System.Linq;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal sealed class WorldHealthGlowController
    {
        private const float HitColorLerpDurationSeconds = 1f;

        private static readonly Color HealthGreen = new(0.2f, 1f, 0.2f, 0.85f);
        private static readonly Color HealthRed = new(0.55f, 0.05f, 0.05f, 0.85f);

        private readonly Dictionary<int, ActiveHealthGlow> _activeGlows = new();
        private readonly WorldOverlayFactory _factory = WorldOverlayFactory.Instance;

        internal bool HasActiveGlows => _activeGlows.Count > 0;

        internal void NotifyDamaged(ProtoActor actor, long damage)
        {
            if (!WorldOverlayGate.HealthGlowEnabled || !WorldOverlayGate.IsWorldDamageTarget(actor))
            {
                return;
            }

            if (!WorldOverlayVisibility.CanShow(actor))
            {
                return;
            }

            float duration = ModConfig.WorldHealthGlowDurationSeconds.Value;
            float now = Time.time;
            int actorId = actor.ActorID;
            float targetPercent = ResolveHealthPercent(actor);
            float previousPercent = ResolvePreviousPercent(actorId, actor, damage, targetPercent);

            if (_activeGlows.TryGetValue(actorId, out ActiveHealthGlow? existing))
            {
                existing.ExpiresAt = now + duration;
                existing.HitAnimStartPercent = previousPercent;
                existing.HitAnimTargetPercent = targetPercent;
                existing.HitAnimStartTime = now;
                return;
            }

            WorldOverlayFactory.HealthGlowWidget widget = _factory.RentHealthGlow();
            widget.Actor = actor;

            ActiveHealthGlow active = new()
            {
                Widget = widget,
                ActorId = actorId,
                ExpiresAt = now + duration,
                HitAnimStartPercent = previousPercent,
                HitAnimTargetPercent = targetPercent,
                HitAnimStartTime = now,
                DisplayPercent = previousPercent,
            };
            ApplyGlowColor(active, previousPercent);
            _activeGlows[actorId] = active;
            _factory.SetRootActive(true);
        }

        internal void Tick(Camera? camera)
        {
            if (_activeGlows.Count == 0)
            {
                return;
            }

            float now = Time.time;
            List<int>? expired = null;

            foreach ((int actorId, ActiveHealthGlow active) in _activeGlows)
            {
                ProtoActor? actor = active.Widget.Actor;
                if (actor == null || actor.dead || !WorldOverlayGate.IsWorldDamageTarget(actor))
                {
                    expired ??= [];
                    expired.Add(actorId);
                    continue;
                }

                if (now >= active.ExpiresAt)
                {
                    expired ??= [];
                    expired.Add(actorId);
                    continue;
                }

                bool visible = WorldOverlayVisibility.CanShow(actor);
                active.Widget.Root.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                float displayPercent = ResolveAnimatedPercent(active, now);
                active.DisplayPercent = displayPercent;
                ApplyGlowColor(active, displayPercent);
                PositionAndBillboard(active.Widget, actor, camera);
            }

            if (expired == null)
            {
                return;
            }

            foreach (int actorId in expired)
            {
                ReleaseGlow(actorId);
            }
        }

        internal void TearDown()
        {
            foreach (int actorId in _activeGlows.Keys.ToList())
            {
                ReleaseGlow(actorId);
            }

            _activeGlows.Clear();
            WorldOverlayHpTracker.Clear();
            WorldOverlayVisibility.ClearCache();
        }

        private void ReleaseGlow(int actorId)
        {
            if (!_activeGlows.TryGetValue(actorId, out ActiveHealthGlow? active))
            {
                return;
            }

            _activeGlows.Remove(actorId);
            WorldOverlayHpTracker.Remove(actorId);
            WorldOverlayVisibility.RemoveFromCache(actorId);
            _factory.ReturnHealthGlow(active.Widget);
        }

        private float ResolvePreviousPercent(
            int actorId,
            ProtoActor actor,
            long damage,
            float targetPercent)
        {
            if (_activeGlows.TryGetValue(actorId, out ActiveHealthGlow? active))
            {
                return ResolveAnimatedPercent(active, Time.time);
            }

            if (damage > 0
                && WorldOverlayHpTracker.TryGetDisplay(actor, out long hp, out long maxHp)
                && maxHp > 0)
            {
                long previousHp = Math.Min(maxHp, hp + damage);
                return Mathf.Clamp01((float)previousHp / maxHp);
            }

            return targetPercent;
        }

        private static float ResolveHealthPercent(ProtoActor actor)
        {
            if (!WorldOverlayHpTracker.TryGetDisplay(actor, out long hp, out long maxHp) || maxHp <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)hp / maxHp);
        }

        private static float ResolveAnimatedPercent(ActiveHealthGlow active, float now)
        {
            float elapsed = now - active.HitAnimStartTime;
            if (elapsed >= HitColorLerpDurationSeconds)
            {
                return active.HitAnimTargetPercent;
            }

            float t = elapsed / HitColorLerpDurationSeconds;
            return Mathf.Lerp(active.HitAnimStartPercent, active.HitAnimTargetPercent, t);
        }

        private static void ApplyGlowColor(ActiveHealthGlow active, float percent)
        {
            Color color = Color.Lerp(HealthRed, HealthGreen, Mathf.Clamp01(percent));
            active.Widget.GlowImage.color = color;
        }

        private static void PositionAndBillboard(
            WorldOverlayFactory.HealthGlowWidget widget,
            ProtoActor actor,
            Camera? camera)
        {
            Vector3 worldPos = WorldOverlayPlacement.ResolveHealthGlowWorldPosition(actor);
            widget.Root.transform.position = worldPos;

            if (camera == null)
            {
                return;
            }

            Vector3 lookDirection = widget.Root.transform.position - camera.transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                widget.Root.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        private sealed class ActiveHealthGlow
        {
            internal WorldOverlayFactory.HealthGlowWidget Widget = null!;
            internal int ActorId;
            internal float ExpiresAt;
            internal float HitAnimStartPercent;
            internal float HitAnimTargetPercent;
            internal float HitAnimStartTime;
            internal float DisplayPercent = 1f;
        }
    }
}
