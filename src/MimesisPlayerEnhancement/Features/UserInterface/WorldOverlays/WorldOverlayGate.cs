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
            if (actor == null)
            {
                return false;
            }

            return WorldOverlayTargetRules.IsEligibleWorldDamageTarget(
                actor.dead,
                actor.AmIAvatar(),
                actor.IsPlayer(),
                actor.ActorType);
        }

        internal static bool IsDamageOverlayTarget(ProtoActor? actor) => IsWorldDamageTarget(actor);
    }
}
