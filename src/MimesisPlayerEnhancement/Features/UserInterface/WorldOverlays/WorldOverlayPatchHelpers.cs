namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayPatchHelpers
    {
        internal static void ProcessHitTargets(GameMainBase? main, List<TargetHitInfo>? hits)
        {
            if (main == null || hits == null)
            {
                return;
            }

            foreach (TargetHitInfo hit in hits)
            {
                if (hit.damage <= 0)
                {
                    continue;
                }

                ProtoActor? victim = main.GetActorByActorID(hit.targetID);
                if (victim == null)
                {
                    continue;
                }

                WorldOverlayRuntime.NotifyHitDamage(victim, hit.damage);
            }
        }
    }
}
