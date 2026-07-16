using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayGate
    {
        internal static bool HealthGlowEnabled { get; private set; } = true;
        internal static bool DamageNumbersEnabled { get; private set; } = true;
        internal static bool DetoxIndicatorsEnabled { get; private set; } = true;

        internal static bool AnyOverlayEnabled =>
            HealthGlowEnabled || DamageNumbersEnabled || DetoxIndicatorsEnabled;

        internal static void RefreshCache()
        {
            HealthGlowEnabled = ModConfig.EnableWorldHealthGlow?.Value ?? false;
            DamageNumbersEnabled = ModConfig.EnableFloatingDamageNumbers?.Value ?? false;
            DetoxIndicatorsEnabled = ModConfig.EnableFloatingDetoxIndicators?.Value ?? false;
        }

        internal static bool IsWorldDamageTarget(ProtoActor? actor)
        {
            if (actor == null || actor.dead || actor.AmIAvatar())
            {
                return false;
            }

            return actor.IsPlayer() || actor.ActorType == ActorType.Monster;
        }

        internal static bool IsDamageOverlayTarget(ProtoActor? actor) => IsWorldDamageTarget(actor);

        internal static bool IsDetoxOverlayTarget(ProtoActor? actor)
        {
            if (actor == null || actor.dead || actor.AmIAvatar())
            {
                return false;
            }

            return actor.IsPlayer() && actor.netSyncActorData.maxConta > 0;
        }
    }
}
