using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayPatchHelpers
    {
        private static int _dedupFrame = -1;
        private static readonly HashSet<(int ActorId, long Damage)> DedupFrameHits = new();

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

                if (!TryConsumeHit(victim.ActorID, hit.damage))
                {
                    continue;
                }

                WorldOverlayRuntime.NotifyHitDamage(victim, hit.damage);
            }
        }

        internal static bool TryConsumeHit(int actorId, long damage)
        {
            int frame = Time.frameCount;
            if (frame != _dedupFrame)
            {
                DedupFrameHits.Clear();
                _dedupFrame = frame;
            }

            return DedupFrameHits.Add((actorId, damage));
        }
    }
}
