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

        internal static bool TryResolveForwardSpawn(
            VPlayer player,
            float minDistanceMeters,
            float maxDistanceMeters,
            out PosWithRot spawnPos)
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
                if (HorizontalDistance(candidate, playerPos) < distance - DistanceStepMeters)
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
                    $"Spawn placement resolved — distance={distance:0.00}m, drop={playerPos.y - floorHit.y:0.00}m.");
                return true;
            }

            ModLog.Debug(Feature, $"Spawn placement blocked — no valid point in {minDistanceMeters:0.00}-{maxDistanceMeters:0.00}m range.");
            return false;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
