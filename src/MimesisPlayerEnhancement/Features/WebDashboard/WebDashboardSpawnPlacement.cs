using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardSpawnPlacement
    {
        private const string Feature = "WebDashboard";

        private const float DistanceStepMeters = 0.5f;
        private const float FloorProbeUpMeters = 1f;
        private const float FloorProbeDownMeters = 4f;
        private const float ObstacleHeightAboveFloorMeters = 0.1f;

        internal static bool TryResolveForwardSpawn(
            VPlayer player,
            float minDistanceMeters,
            float maxDistanceMeters,
            out PosWithRot spawnPos)
        {
            spawnPos = new PosWithRot();

            Vector3 playerPos = player.PositionVector;
            if (!TryResolveHorizontalLook(player, playerPos, out Vector3 forward, out float lookYaw))
            {
                return false;
            }

            float facingYaw = lookYaw - 180f;

            for (float distance = maxDistanceMeters;
                 distance >= minDistanceMeters - 0.001f;
                 distance -= DistanceStepMeters)
            {
                Vector3 horizontal = playerPos + forward * distance;
                Vector3 probeOrigin = new(horizontal.x, playerPos.y, horizontal.z);
                Vector3 floorHit = PhysicsUtility.DropToFloor(
                    probeOrigin,
                    margin: FloorProbeUpMeters,
                    maxDistance: FloorProbeDownMeters);
                if (floorHit == probeOrigin)
                {
                    continue;
                }

                Vector3 spawn = new(floorHit.x, floorHit.y, floorHit.z);
                Vector3 spawnProbe = floorHit + Vector3.up * ObstacleHeightAboveFloorMeters;
                Vector3 playerProbe = new(playerPos.x, spawnProbe.y, playerPos.z);

                // CheckBlockByWall returns true when the path is clear (no hit).
                if (!PhysicsUtility.CheckBlockByWall(playerProbe, spawnProbe))
                {
                    continue;
                }

                spawnPos = spawn.toPosWithRot(0f);
                spawnPos.yaw = facingYaw;
                ModLog.Debug(
                    Feature,
                    $"Spawn placement resolved — distance={distance:0.00}m, drop={playerPos.y - floorHit.y:0.00}m, lookYaw={lookYaw:0.0}.");
                return true;
            }

            ModLog.Debug(
                Feature,
                $"Spawn placement blocked — no valid point in {minDistanceMeters:0.00}-{maxDistanceMeters:0.00}m range.");
            return false;
        }

        private static bool TryResolveHorizontalLook(
            VPlayer player,
            Vector3 playerPos,
            out Vector3 forward,
            out float lookYaw)
        {
            if (TryGetFpvCameraRoot(player, out Transform camRoot))
            {
                Vector3 flat = camRoot.forward;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.0001f)
                {
                    forward = flat.normalized;
                    lookYaw = ResolveHorizontalYaw(camRoot);
                    return true;
                }
            }

            float bodyYaw = player.Position.yaw;
            Vector3 bodyForward = Misc.GetPosWithAngleDistance(playerPos, bodyYaw, 1f) - playerPos;
            bodyForward.y = 0f;
            if (bodyForward.sqrMagnitude < 0.0001f)
            {
                forward = default;
                lookYaw = 0f;
                return false;
            }

            forward = bodyForward.normalized;
            lookYaw = bodyYaw;
            return true;
        }

        private static bool TryGetFpvCameraRoot(VPlayer player, out Transform camRoot)
        {
            camRoot = null!;
            GameMainBase? main = GameSessionAccess.TryGetPdata()?.main;
            if (main?.GetActorByPlayerUID(player.UID) is not ProtoActor actor)
            {
                return false;
            }

            Transform? root = actor.FpvCameraRoot;
            if (root == null)
            {
                return false;
            }

            camRoot = root;
            return true;
        }

        private static float ResolveHorizontalYaw(Transform transform)
        {
            Vector3 flat = transform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude <= 0.0001f)
            {
                return transform.eulerAngles.y;
            }

            return Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        }
    }
}
