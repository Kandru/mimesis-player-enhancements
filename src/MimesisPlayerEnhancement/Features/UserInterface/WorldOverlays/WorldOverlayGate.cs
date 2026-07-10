using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayGate
    {
        internal static bool HealthBarsEnabled { get; private set; } = true;
        internal static bool DamageNumbersEnabled { get; private set; } = true;
        internal static bool DetoxIndicatorsEnabled { get; private set; } = true;

        internal static bool AnyOverlayEnabled =>
            HealthBarsEnabled || DamageNumbersEnabled || DetoxIndicatorsEnabled;

        internal static void RefreshCache()
        {
            HealthBarsEnabled = ModConfig.EnableWorldHealthBars?.Value ?? false;
            DamageNumbersEnabled = ModConfig.EnableFloatingDamageNumbers?.Value ?? false;
            DetoxIndicatorsEnabled = ModConfig.EnableFloatingDetoxIndicators?.Value ?? false;
        }

        internal static bool IsHealthBarTarget(ProtoActor? actor)
        {
            if (actor == null || actor.dead || actor.AmIAvatar())
            {
                return false;
            }

            return actor.IsPlayer() || actor.ActorType == ActorType.Monster;
        }

        internal static bool IsDamageOverlayTarget(ProtoActor? actor) => IsHealthBarTarget(actor);

        internal static bool IsDetoxOverlayTarget(ProtoActor? actor)
        {
            return actor != null && !actor.dead && actor.IsPlayer() && actor.netSyncActorData.maxConta > 0;
        }
    }
}
