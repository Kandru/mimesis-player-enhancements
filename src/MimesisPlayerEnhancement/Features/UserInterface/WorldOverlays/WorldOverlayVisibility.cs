using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayVisibility
    {
        internal const float MaxViewDistanceMeters = 20f;

        private const float ViewerEyeHeight = 1.5f;
        private const float TargetCheckHeight = 1.5f;
        private const float RecheckIntervalSeconds = 0.1f;

        private static readonly float MaxViewDistanceSqr = MaxViewDistanceMeters * MaxViewDistanceMeters;
        private static readonly Dictionary<int, CachedResult> ActorCache = new();

        internal static bool CanShow(ProtoActor? actor)
        {
            if (actor == null || actor.dead)
            {
                return false;
            }

            return CanShowCached(actor.ActorID, actor.transform.position, TargetCheckHeight);
        }

        internal static bool CanShow(Vector3 targetBasePosition, float targetHeight = TargetCheckHeight)
        {
            return Evaluate(targetBasePosition, targetHeight);
        }

        /// <summary>Actor-keyed LOS with the same 0.1s cache as <see cref="CanShow(ProtoActor)"/>,
        /// for floaters that track a moving world position under a stable actor id.</summary>
        internal static bool CanShow(int actorId, Vector3 targetBasePosition, float targetHeight = TargetCheckHeight)
        {
            return CanShowCached(actorId, targetBasePosition, targetHeight);
        }

        internal static void ClearCache()
        {
            ActorCache.Clear();
        }

        internal static void RemoveFromCache(int actorId)
        {
            ActorCache.Remove(actorId);
        }

        private static bool CanShowCached(int actorId, Vector3 targetBasePosition, float targetHeight)
        {
            float now = Time.time;
            if (ActorCache.TryGetValue(actorId, out CachedResult cached) && now < cached.NextCheckTime)
            {
                return cached.Visible;
            }

            bool visible = Evaluate(targetBasePosition, targetHeight);
            ActorCache[actorId] = new CachedResult
            {
                NextCheckTime = now + RecheckIntervalSeconds,
                Visible = visible,
            };
            return visible;
        }

        private static bool Evaluate(Vector3 targetBasePosition, float targetHeight)
        {
            Vector3? viewerPosition = WorldOverlayViewer.TryGetWorldPosition();
            if (!viewerPosition.HasValue)
            {
                return false;
            }

            Vector3 viewerEye = viewerPosition.Value + Vector3.up * ViewerEyeHeight;
            Vector3 targetPoint = targetBasePosition + Vector3.up * targetHeight;

            if ((targetPoint - viewerEye).sqrMagnitude > MaxViewDistanceSqr)
            {
                return false;
            }

            return PhysicsUtility.IsVisible(viewerEye, targetPoint);
        }

        private sealed class CachedResult
        {
            internal float NextCheckTime;
            internal bool Visible;
        }
    }
}
