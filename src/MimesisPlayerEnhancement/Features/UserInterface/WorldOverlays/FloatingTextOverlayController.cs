using MimesisPlayerEnhancement.Ui;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal sealed class FloatingTextOverlayController
    {
        private const int MaxConcurrentFloaters = 24;
        private const float RiseSpeed = 0.75f;
        private const float DriftSpeed = 1.25f;
        private const float FadeInFraction = 0.15f;
        private const float VerticalOffset = 1.6f;
        private const float DedupWindowSeconds = 0.15f;

        private readonly Func<bool> _isEnabled;
        private readonly Func<ProtoActor, bool> _isTarget;
        private readonly bool _requiresVisibility;
        private readonly WorldOverlayFactory _factory = WorldOverlayFactory.Instance;

        private readonly List<ActiveFloater> _activeFloaters = [];
        private int _lastActorId = -1;
        private string _lastText = "";
        private float _lastSpawnTime;

        internal FloatingTextOverlayController(
            Func<bool> isEnabled,
            Func<ProtoActor, bool> isTarget,
            bool requiresVisibility = false)
        {
            _isEnabled = isEnabled;
            _isTarget = isTarget;
            _requiresVisibility = requiresVisibility;
        }

        internal bool HasActiveFloaters => _activeFloaters.Count > 0;

        internal void Spawn(ProtoActor actor, string text, Color color)
        {
            if (!_isEnabled() || !_isTarget(actor))
            {
                return;
            }

            if (_requiresVisibility && !WorldOverlayVisibility.CanShow(actor))
            {
                return;
            }

            if (_activeFloaters.Count >= MaxConcurrentFloaters)
            {
                RemoveFloaterAt(0);
            }

            int actorId = actor.ActorID;
            float now = Time.time;
            if (actorId == _lastActorId
                && text == _lastText
                && now - _lastSpawnTime < DedupWindowSeconds)
            {
                return;
            }

            _lastActorId = actorId;
            _lastText = text;
            _lastSpawnTime = now;

            float duration = ModConfig.FloatingDamageDurationSeconds.Value;
            Vector3 startPosition = actor.transform.position + Vector3.up * VerticalOffset;
            Vector3 driftDirection = WorldOverlayViewer.ResolveDriftDirection(startPosition);

            WorldOverlayFactory.FloaterWidget widget = _factory.RentFloater();
            ModUiText.SetText(widget.TextComponent, text);
            widget.BaseColor = color;
            ModUiText.SetColor(widget.TextComponent, color);

            _activeFloaters.Add(new ActiveFloater
            {
                Widget = widget,
                ActorId = actorId,
                StartTime = now,
                Duration = duration,
                StartPosition = startPosition,
                DriftDirection = driftDirection,
                TextColor = color,
            });

            widget.Root.transform.position = startPosition;
            _factory.SetRootActive(true);
        }

        internal void Tick(Camera? camera)
        {
            if (_activeFloaters.Count == 0)
            {
                return;
            }

            float now = Time.time;

            for (int i = _activeFloaters.Count - 1; i >= 0; i--)
            {
                ActiveFloater active = _activeFloaters[i];
                float elapsed = now - active.StartTime;
                if (elapsed >= active.Duration)
                {
                    RemoveFloaterAt(i);
                    continue;
                }

                float normalized = elapsed / active.Duration;
                float fadeInEnd = FadeInFraction;
                float alpha = normalized < fadeInEnd
                    ? normalized / fadeInEnd
                    : 1f - ((normalized - fadeInEnd) / (1f - fadeInEnd));

                Vector3 position = active.StartPosition
                    + active.DriftDirection * (DriftSpeed * elapsed)
                    + Vector3.up * (RiseSpeed * elapsed);

                bool visible = !_requiresVisibility || WorldOverlayVisibility.CanShow(position);
                active.Widget.Root.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                active.Widget.Root.transform.position = position;
                WorldOverlayViewer.BillboardTowardCamera(active.Widget.Root.transform, position, camera);

                Color color = active.TextColor;
                color.a = Mathf.Clamp01(alpha);
                ModUiText.SetColor(active.Widget.TextComponent, color);
            }
        }

        internal void TearDown()
        {
            for (int i = _activeFloaters.Count - 1; i >= 0; i--)
            {
                RemoveFloaterAt(i);
            }

            _activeFloaters.Clear();
        }

        private void RemoveFloaterAt(int index)
        {
            ActiveFloater active = _activeFloaters[index];
            _activeFloaters.RemoveAt(index);
            _factory.ReturnFloater(active.Widget);
        }

        private sealed class ActiveFloater
        {
            internal WorldOverlayFactory.FloaterWidget Widget = null!;
            internal int ActorId;
            internal float StartTime;
            internal float Duration;
            internal Vector3 StartPosition;
            internal Vector3 DriftDirection;
            internal Color TextColor;
        }
    }
}
