using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayRuntime
    {
        private static readonly Color DamageTextColor = new(1f, 0.25f, 0.25f, 1f);
        private static readonly Color DetoxTextColor = new(0.25f, 0.9f, 0.3f, 1f);

        private static readonly WorldHealthGlowController HealthGlows = new();
        private static readonly FloatingTextOverlayController DamageFloaters = new(
            () => WorldOverlayGate.DamageNumbersEnabled,
            WorldOverlayGate.IsDamageOverlayTarget,
            requiresVisibility: true);
        private static readonly FloatingTextOverlayController DetoxFloaters = new(
            () => WorldOverlayGate.DetoxIndicatorsEnabled,
            WorldOverlayGate.IsDetoxOverlayTarget);

        private static bool _hasActiveOverlays;

        internal static bool HasActiveOverlays => _hasActiveOverlays;

        internal static void OnUpdate()
        {
            RefreshActiveFlag();
            if (!_hasActiveOverlays)
            {
                return;
            }

            if (!WorldOverlayGate.AnyOverlayEnabled)
            {
                TearDownAll();
                return;
            }

            Camera? camera = ResolveCamera();
            if (WorldOverlayGate.HealthGlowEnabled)
            {
                HealthGlows.Tick(camera);
            }

            if (WorldOverlayGate.DamageNumbersEnabled)
            {
                DamageFloaters.Tick(camera);
            }

            if (WorldOverlayGate.DetoxIndicatorsEnabled)
            {
                DetoxFloaters.Tick(camera);
            }

            RefreshActiveFlag();
            if (!_hasActiveOverlays)
            {
                WorldOverlayFactory.Instance.SetRootActive(false);
            }
        }

        internal static void RefreshFromConfig()
        {
            WorldOverlayGate.RefreshCache();
            if (!WorldOverlayGate.HealthGlowEnabled)
            {
                HealthGlows.TearDown();
            }

            if (!WorldOverlayGate.DamageNumbersEnabled)
            {
                DamageFloaters.TearDown();
            }

            if (!WorldOverlayGate.DetoxIndicatorsEnabled)
            {
                DetoxFloaters.TearDown();
            }

            if (!WorldOverlayGate.AnyOverlayEnabled)
            {
                TearDownAll();
                return;
            }

            RefreshActiveFlag();
        }

        internal static void NotifyHitDamage(ProtoActor victim, long damage)
        {
            if (damage <= 0 || !WorldOverlayGate.IsWorldDamageTarget(victim))
            {
                return;
            }

            WorldOverlayHpTracker.ApplyDamage(victim, damage);
            NotifyEntityDamaged(victim, damage);
        }

        internal static void NotifyContaReduced(ProtoActor actor, long previousConta, long newConta, long maxConta)
        {
            if (newConta >= previousConta || !WorldOverlayGate.IsDetoxOverlayTarget(actor))
            {
                return;
            }

            long reduction = previousConta - newConta;
            int percent = maxConta > 0
                ? (int)Math.Round(reduction * 100.0 / maxConta, MidpointRounding.AwayFromZero)
                : 0;
            if (percent <= 0)
            {
                return;
            }

            DetoxFloaters.Spawn(
                actor,
                $"-{percent}%",
                DetoxTextColor,
                displayScale: WorldOverlayFactory.FloaterScale);
            RefreshActiveFlag();
        }

        private static void NotifyEntityDamaged(ProtoActor actor, long damage)
        {
            if (WorldOverlayGate.HealthGlowEnabled && WorldOverlayGate.IsWorldDamageTarget(actor))
            {
                HealthGlows.NotifyDamaged(actor, damage);
            }

            if (WorldOverlayGate.DamageNumbersEnabled
                && damage > 0
                && WorldOverlayGate.IsDamageOverlayTarget(actor))
            {
                DamageFloaters.Spawn(
                    actor,
                    $"-{damage}",
                    DamageTextColor,
                    displayScale: WorldOverlayFactory.FloaterScale);
            }

            RefreshActiveFlag();
        }

        private static void TearDownAll()
        {
            HealthGlows.TearDown();
            DamageFloaters.TearDown();
            DetoxFloaters.TearDown();
            WorldOverlayHpTracker.Clear();
            WorldOverlayVisibility.ClearCache();
            WorldOverlayFactory.Instance.SetRootActive(false);
            _hasActiveOverlays = false;
        }

        private static void RefreshActiveFlag()
        {
            _hasActiveOverlays =
                (WorldOverlayGate.HealthGlowEnabled && HealthGlows.HasActiveGlows)
                || (WorldOverlayGate.DamageNumbersEnabled && DamageFloaters.HasActiveFloaters)
                || (WorldOverlayGate.DetoxIndicatorsEnabled && DetoxFloaters.HasActiveFloaters);
        }

        private static Camera? _cachedFallbackCamera;

        private static Camera? ResolveCamera()
        {
            if (Camera.main != null)
            {
                return Camera.main;
            }

            if (_cachedFallbackCamera != null)
            {
                return _cachedFallbackCamera;
            }

            Camera[] cameras = Camera.allCameras;
            _cachedFallbackCamera = cameras.Length > 0 ? cameras[0] : null;
            return _cachedFallbackCamera;
        }
    }
}
