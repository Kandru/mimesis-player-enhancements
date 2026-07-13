using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayRuntime
    {
        private static readonly Color DamageTextColor = new(1f, 0.25f, 0.25f, 1f);
        private static readonly Color DetoxTextColor = new(0.25f, 0.9f, 0.3f, 1f);

        private static readonly WorldHealthBarController HealthBars = new();
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
            if (WorldOverlayGate.HealthBarsEnabled)
            {
                HealthBars.Tick(camera);
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
            if (!WorldOverlayGate.HealthBarsEnabled)
            {
                HealthBars.TearDown();
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
            if (damage <= 0 || !WorldOverlayGate.IsHealthBarTarget(victim))
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
            if (WorldOverlayGate.HealthBarsEnabled && WorldOverlayGate.IsHealthBarTarget(actor))
            {
                HealthBars.NotifyDamaged(actor);
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
            HealthBars.TearDown();
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
                (WorldOverlayGate.HealthBarsEnabled && HealthBars.HasActiveBars)
                || (WorldOverlayGate.DamageNumbersEnabled && DamageFloaters.HasActiveFloaters)
                || (WorldOverlayGate.DetoxIndicatorsEnabled && DetoxFloaters.HasActiveFloaters);
        }

        private static Camera? ResolveCamera()
        {
            if (Camera.main != null)
            {
                return Camera.main;
            }

            Camera[] cameras = Camera.allCameras;
            return cameras.Length > 0 ? cameras[0] : null;
        }
    }
}
