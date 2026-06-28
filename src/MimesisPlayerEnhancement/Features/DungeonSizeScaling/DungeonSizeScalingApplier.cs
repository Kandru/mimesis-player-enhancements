using System;
using System.Collections.Generic;
using DunGen;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.DungeonSizeScaling;

internal static class DungeonSizeScalingApplier
{
    private static readonly HashSet<DungeonGenerator> ScaledGenerators = new();

    internal static void ApplyBeforeGenerate(DungeonGenerator generator)
    {
        if (!ModConfig.EnableDungeonSizeScaling.Value)
            return;

        if (ScaledGenerators.Contains(generator))
            return;

        int playerCount = SessionPlayerCountHelper.ResolveFromSession();
        float scale = DungeonSizeScalingResolver.GetLengthMultiplier(playerCount);
        if (Math.Abs(scale - 1f) < 0.0001f)
        {
            ScaledGenerators.Add(generator);
            DungeonSizeScalingLog.Debug($"No size bonus for players={playerCount}");
            return;
        }

        float previous = generator.LengthMultiplier;
        generator.LengthMultiplier = previous * scale;
        ScaledGenerators.Add(generator);
        DungeonSizeScalingLog.Info(playerCount, scale, previous, generator.LengthMultiplier);
    }

    internal static void OnGeneratorCleared(DungeonGenerator generator)
    {
        ScaledGenerators.Remove(generator);
    }
}
