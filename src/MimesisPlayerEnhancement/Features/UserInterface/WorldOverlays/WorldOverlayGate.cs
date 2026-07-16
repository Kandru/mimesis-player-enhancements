using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayGate
    {
        internal static bool DamageHealthGlowEnabled { get; private set; } = true;
        internal static bool DamageNumbersEnabled { get; private set; } = true;

        internal static bool AnyOverlayEnabled =>
            DamageHealthGlowEnabled || DamageNumbersEnabled;

        internal static void RefreshCache()
        {
            DamageHealthGlowEnabled = ModConfig.EnableDamageHealthGlow?.Value ?? false;
            DamageNumbersEnabled = ModConfig.EnableFloatingDamageNumbers?.Value ?? false;
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
    }
}
