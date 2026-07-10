using System.Linq;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal sealed class WorldHealthBarController
    {
        private const float HitFlashDurationSeconds = 0.4f;
        private const float HitPulseScale = 1.12f;

        private readonly Dictionary<int, ActiveHealthBar> _activeBars = new();
        private readonly WorldOverlayFactory _factory = WorldOverlayFactory.Instance;

        internal bool HasActiveBars => _activeBars.Count > 0;

        internal void NotifyDamaged(ProtoActor actor)
        {
            if (!WorldOverlayGate.HealthBarsEnabled || !WorldOverlayGate.IsHealthBarTarget(actor))
            {
                return;
            }

            if (!WorldOverlayVisibility.CanShow(actor))
            {
                return;
            }

            float duration = ModConfig.WorldHealthBarDurationSeconds.Value;
            float now = Time.time;
            int actorId = actor.ActorID;

            if (_activeBars.TryGetValue(actorId, out ActiveHealthBar? existing))
            {
                existing.ExpiresAt = now + duration;
                existing.HitFlashUntil = now + HitFlashDurationSeconds;
                UpdateBarValues(existing, actor);
                return;
            }

            WorldOverlayFactory.HealthBarWidget widget = _factory.RentHealthBar();
            widget.Actor = actor;

            ActiveHealthBar active = new()
            {
                Widget = widget,
                ActorId = actorId,
                ExpiresAt = now + duration,
                HitFlashUntil = now + HitFlashDurationSeconds,
            };
            UpdateBarValues(active, actor);
            _activeBars[actorId] = active;
            _factory.SetRootActive(true);
        }

        internal void Tick(Camera? camera)
        {
            if (_activeBars.Count == 0)
            {
                return;
            }

            float now = Time.time;
            List<int>? expired = null;

            foreach ((int actorId, ActiveHealthBar active) in _activeBars)
            {
                ProtoActor? actor = active.Widget.Actor;
                if (actor == null || actor.dead || !WorldOverlayGate.IsHealthBarTarget(actor))
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

                UpdateBarValues(active, actor);
                ApplyHitFlash(active, now);
                PositionAndBillboard(active.Widget, actor, camera);
            }

            if (expired == null)
            {
                return;
            }

            foreach (int actorId in expired)
            {
                ReleaseBar(actorId);
            }
        }

        internal void TearDown()
        {
            foreach (int actorId in _activeBars.Keys.ToList())
            {
                ReleaseBar(actorId);
            }

            _activeBars.Clear();
            WorldOverlayHpTracker.Clear();
            WorldOverlayVisibility.ClearCache();
        }

        private void ReleaseBar(int actorId)
        {
            if (!_activeBars.TryGetValue(actorId, out ActiveHealthBar? active))
            {
                return;
            }

            _activeBars.Remove(actorId);
            WorldOverlayHpTracker.Remove(actorId);
            WorldOverlayVisibility.RemoveFromCache(actorId);
            _factory.ReturnHealthBar(active.Widget);
        }

        private static void UpdateBarValues(ActiveHealthBar active, ProtoActor actor)
        {
            if (!WorldOverlayHpTracker.TryGetDisplay(actor, out long hp, out long maxHp))
            {
                active.Widget.SetFillPercent(0f);
                ModUiText.SetText(active.Widget.TextComponent, "--");
                return;
            }

            float percent = Mathf.Clamp01((float)hp / maxHp);

            active.Widget.SetFillPercent(percent);
            ModUiText.SetText(active.Widget.TextComponent, $"{hp}/{maxHp}");
        }

        private static void ApplyHitFlash(ActiveHealthBar active, float now)
        {
            WorldOverlayFactory.HealthBarWidget widget = active.Widget;
            if (now < active.HitFlashUntil)
            {
                float t = 1f - ((active.HitFlashUntil - now) / HitFlashDurationSeconds);
                float pulse = Mathf.Lerp(HitPulseScale, 1f, t);
                widget.RootTransform.localScale = new Vector3(pulse, pulse, 1f);
                widget.FillImage.color = Color.Lerp(widget.FlashFillColor, widget.BaseFillColor, t);
                return;
            }

            widget.RootTransform.localScale = Vector3.one;
            widget.FillImage.color = widget.BaseFillColor;
        }

        private static void PositionAndBillboard(
            WorldOverlayFactory.HealthBarWidget widget,
            ProtoActor actor,
            Camera? camera)
        {
            Vector3 worldPos = WorldOverlayPlacement.ResolveHealthBarWorldPosition(actor);
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

        private sealed class ActiveHealthBar
        {
            internal WorldOverlayFactory.HealthBarWidget Widget = null!;
            internal int ActorId;
            internal float ExpiresAt;
            internal float HitFlashUntil;
        }
    }
}
