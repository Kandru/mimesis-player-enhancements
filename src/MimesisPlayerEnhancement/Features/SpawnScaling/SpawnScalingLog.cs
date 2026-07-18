using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnScalingLog
    {
        private const string Feature = "SpawnScaling";

        private static string FormatPosition(Vector3 pos)
        {
            return $"({pos.x:0.0}, {pos.y:0.0}, {pos.z:0.0})";
        }

        internal static string FormatLocation(DungeonRoom? room, Vector3 pos)
        {
            string location = FormatPosition(pos);
            if (room == null)
            {
                return location;
            }

            string roomName = SpawnScalingRoomLookup.TryGetRoomName(room, pos);
            return string.IsNullOrWhiteSpace(roomName)
                ? $"{location} room=(unknown)"
                : $"{location} room={roomName}";
        }

        internal static void InfoScalingApplied(int playerCount, SpawnScalingSceneConfig config)
        {
            float sharedPlayerScale = ScalingMath.GetPlayerScale(
                playerCount,
                autoScaleEnabled: true,
                config.SpawnScalingPlayerCountScaleRate);
            ModLog.Info(
                Feature,
                $"Spawn scaling applied — players={playerCount} (shared playerScale={sharedPlayerScale:0.##}× at rate={config.SpawnScalingPlayerCountScaleRate:0.##} when auto enabled), " +
                $"mimic={config.MimicSpawnMultiplier:0.##}× auto={config.AutoScaleMimicSpawnsByPlayerCount}, " +
                $"boss={config.BossSpawnMultiplier:0.##}× auto={config.AutoScaleBossSpawnsByPlayerCount}, " +
                $"jako={config.JakoSpawnMultiplier:0.##}× auto={config.AutoScaleJakoSpawnsByPlayerCount}, " +
                $"special={config.SpecialSpawnMultiplier:0.##}× auto={config.AutoScaleSpecialSpawnsByPlayerCount}, " +
                $"trap={config.TrapSpawnMultiplier:0.##}× auto={config.AutoScaleTrapSpawnsByPlayerCount}, " +
                $"other={config.OtherSpawnMultiplier:0.##}× auto={config.AutoScaleOtherSpawnsByPlayerCount}");
        }

        internal static void DebugFieldScaled(string label, int before, int after, float multiplier)
        {
            // Early-return before building strings — called for every scaled field on Apply().
            if (!ModConfig.EnableDebugLogging.Value)
            {
                return;
            }

            if (before == after)
            {
                ModLog.Debug(Feature, $"{label} unchanged at {before} ({multiplier:0.##}×)");
                return;
            }

            ModLog.Debug(Feature, $"{label} scaled {before} -> {after} ({multiplier:0.##}×)");
        }

        internal static void DebugEntitySpawned(
            DungeonRoom room,
            int masterId,
            string entityName,
            SpawnCategory category,
            float effectiveMultiplier,
            bool scalingApplied,
            Vector3 position,
            bool isIndoor,
            ReasonOfSpawn reason,
            string spawnSource)
        {
            ModLog.Debug(
                Feature,
                $"Entity spawned — category={SpawnCategoryLookup.Format(category)}, name={entityName}, master={masterId}, " +
                $"multiplier={effectiveMultiplier:0.##}×, budgetsScaled={scalingApplied}, pos={FormatLocation(room, position)}, " +
                $"indoor={isIndoor}, reason={reason}, source={spawnSource}");
        }

        internal static void DebugSpawnFailed(
            int masterId,
            string entityName,
            SpawnCategory category,
            bool scalingApplied,
            string spawnSource)
        {
            ModLog.Debug(
                Feature,
                $"Entity spawn failed — category={SpawnCategoryLookup.Format(category)}, name={entityName}, " +
                $"master={masterId}, budgetsScaled={scalingApplied}, source={spawnSource}");
        }

        internal static void InfoPeriodicSpawnWaitApplied(
            PeriodicSpawnWaitMode mode,
            float initialSeconds,
            float intervalSeconds)
        {
            ModLog.Info(
                Feature,
                $"Periodic spawn wait applied — mode={mode}, initial={initialSeconds:0.#}s, interval={intervalSeconds:0.#}s");
        }

        internal static void DebugPeriodicSpawnIntervalRerolled(string waveKind, float intervalSeconds)
        {
            ModLog.Debug(Feature, $"Periodic spawn interval re-rolled — wave={waveKind}, interval={intervalSeconds:0.#}s");
        }
    }
}
