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
            ModLog.Info(
                Feature,
                $"Spawn scaling applied — players={playerCount}, baseline={config.SpawnScalingBaselinePlayerCount}, " +
                $"mimic={SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Mimic, playerCount, config):0.##}× " +
                $"(base={config.MimicSpawnMultiplier:0.##}×, perPlayer={config.MimicSpawnPerPlayerMultiplier:0.##}), " +
                $"boss={SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Boss, playerCount, config):0.##}× " +
                $"(base={config.BossSpawnMultiplier:0.##}×, perPlayer={config.BossSpawnPerPlayerMultiplier:0.##}), " +
                $"jako={SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Jako, playerCount, config):0.##}× " +
                $"(base={config.JakoSpawnMultiplier:0.##}×, perPlayer={config.JakoSpawnPerPlayerMultiplier:0.##}), " +
                $"special={SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Special, playerCount, config):0.##}× " +
                $"(base={config.SpecialSpawnMultiplier:0.##}×, perPlayer={config.SpecialSpawnPerPlayerMultiplier:0.##}), " +
                $"trap={SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Trap, playerCount, config):0.##}× " +
                $"(base={config.TrapSpawnMultiplier:0.##}×, perPlayer={config.TrapSpawnPerPlayerMultiplier:0.##}), " +
                $"other={SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Other, playerCount, config):0.##}× " +
                $"(base={config.OtherSpawnMultiplier:0.##}×, perPlayer={config.OtherSpawnPerPlayerMultiplier:0.##})");
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

        internal static void InfoAmbientMonsterWaveApplied(
            AmbientMonsterWaveMode mode,
            float initialSeconds,
            float intervalSeconds)
        {
            ModLog.Info(
                Feature,
                $"Ambient monster wave timing applied — mode={mode}, initial={initialSeconds:0.#}s, interval={intervalSeconds:0.#}s");
        }

        internal static void DebugAmbientMonsterWaveIntervalRerolled(string waveKind, float intervalSeconds)
        {
            ModLog.Debug(Feature, $"Ambient monster wave interval re-rolled — wave={waveKind}, interval={intervalSeconds:0.#}s");
        }
    }
}
