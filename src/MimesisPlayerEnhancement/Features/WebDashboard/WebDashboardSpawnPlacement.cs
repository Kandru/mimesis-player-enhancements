using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardSpawnPlacement
    {
        private const string Feature = "WebDashboard";

        private const float MaxFloorDropMeters = 0.5f;
        private const float FloorProbeMarginMeters = 1f;
        private const float WallClearanceMeters = 0.5f;
        private const float DistanceStepMeters = 0.25f;
        private const float BlockCheckRadiusMeters = 0.4f;
        private const float PlayerNavSnapRadiusMeters = 2f;
        private const float HoverHeadroomMeters = 1.5f;
        private const float FloorProbeEpsilonMeters = 0.05f;

        internal static bool TryResolveForwardSpawn(
            VPlayer player,
            float minDistanceMeters,
            float maxDistanceMeters,
            out PosWithRot spawnPos,
            float hoverHeightMeters = 0f)
        {
            spawnPos = new PosWithRot();

            VWorld? vworld = GameSessionAccess.TryGetVWorld();
            if (vworld == null)
            {
                return false;
            }

            Vector3 playerPos = player.PositionVector;
            float yaw = player.Position.yaw;
            float facingYaw = yaw - 180f;

            Vector3 nearestPlayer = vworld.FindNearestPoly(playerPos, PlayerNavSnapRadiusMeters);
            if (!NavMeshConstants.IsValid(nearestPlayer))
            {
                ModLog.Debug(Feature, "Spawn placement blocked — player off navmesh.");
                return false;
            }

            Vector3 forwardPoint = Misc.GetPosWithAngleDistance(playerPos, yaw, 1f);
            Vector3 forward = forwardPoint - playerPos;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            forward.Normalize();

            float wallDistance = vworld.DistanceToWall(
                playerPos,
                forward,
                maxDistanceMeters + WallClearanceMeters);
            if (wallDistance < minDistanceMeters + WallClearanceMeters)
            {
                ModLog.Debug(Feature, $"Spawn placement blocked — wallDistance={wallDistance:0.00}m.");
                return false;
            }

            float startDistance = Mathf.Clamp(
                wallDistance - WallClearanceMeters,
                minDistanceMeters,
                maxDistanceMeters);

            for (float distance = startDistance;
                 distance >= minDistanceMeters - 0.001f;
                 distance -= DistanceStepMeters)
            {
                Vector3 candidate = vworld.GetReachableDistancePos(playerPos, yaw, distance);
                float requiredReach = distance - DistanceStepMeters;
                if (requiredReach > 0f
                    && HorizontalDistanceSq(candidate, playerPos) < requiredReach * requiredReach)
                {
                    continue;
                }

                Vector3 probeOrigin = new(candidate.x, playerPos.y, candidate.z);
                Vector3 floorHit = PhysicsUtility.DropToFloor(
                    probeOrigin,
                    margin: FloorProbeMarginMeters,
                    maxDistance: FloorProbeMarginMeters + MaxFloorDropMeters);
                if (floorHit == probeOrigin)
                {
                    continue;
                }

                if (playerPos.y - floorHit.y > MaxFloorDropMeters)
                {
                    continue;
                }

                Vector3 placed = new(candidate.x, floorHit.y, candidate.z);
                float appliedHover = 0f;
                if (hoverHeightMeters > 0f
                    && TryResolveHoverSpawn(floorHit, hoverHeightMeters, out Vector3 hoverPos))
                {
                    placed = hoverPos;
                    appliedHover = hoverHeightMeters;
                }

                if (vworld.IsFullyBlockedByWall(
                        playerPos,
                        placed,
                        BlockCheckRadiusMeters,
                        BlockCheckRadiusMeters))
                {
                    continue;
                }

                spawnPos = placed.toPosWithRot(0f);
                spawnPos.yaw = facingYaw;
                ModLog.Debug(
                    Feature,
                    $"Spawn placement resolved — distance={distance:0.00}m, drop={playerPos.y - floorHit.y:0.00}m, hover={appliedHover:0.00}m.");
                return true;
            }

            ModLog.Debug(Feature, $"Spawn placement blocked — no valid point in {minDistanceMeters:0.00}-{maxDistanceMeters:0.00}m range.");
            return false;
        }

        private static bool TryResolveHoverSpawn(Vector3 floorHit, float hoverHeightMeters, out Vector3 hoverPos)
        {
            hoverPos = new Vector3(floorHit.x, floorHit.y + hoverHeightMeters, floorHit.z);

            // Clear air from just above the floor through hover + monster headroom.
            Vector3 upOrigin = floorHit + Vector3.up * FloorProbeEpsilonMeters;
            Vector3 upTarget = floorHit + Vector3.up * (hoverHeightMeters + HoverHeadroomMeters);
            if (!PhysicsUtility.CheckBlockByWall(upOrigin, upTarget))
            {
                return false;
            }

            // Confirm open space under the hover point so the actor is not embedded in the floor.
            Vector3 floorBelow = PhysicsUtility.DropToFloor(
                hoverPos,
                margin: FloorProbeEpsilonMeters,
                maxDistance: hoverHeightMeters + 0.2f);
            if (floorBelow == hoverPos)
            {
                return false;
            }

            float gap = hoverPos.y - floorBelow.y;
            return gap >= hoverHeightMeters - 0.1f && gap <= hoverHeightMeters + 0.15f;
        }

        private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
