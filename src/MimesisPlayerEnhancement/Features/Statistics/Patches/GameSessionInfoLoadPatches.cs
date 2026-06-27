using HarmonyLib;
using Mimic.Actors;
using MimesisPlayerEnhancement.Features.Persistence;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches;

[HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.ApplyLoadedGameData))]
public static class GameSessionInfoLoadPatches
{
    [HarmonyPostfix]
    public static void Postfix(MMSaveGameData data)
    {
        if (!ModConfig.EnableStatistics.Value)
            return;
        if (!MimesisSaveManager.IsHost())
            return;

        int slotId = MimesisSaveManager.GetCurrentSaveSlotId();
        if (!MMSaveGameData.CheckSaveSlotID(slotId, true))
            return;

        StatisticsTracker.LoadForSlot(slotId);
    }
}

[HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.OnPlayerDeath))]
public static class GameMainDeathPatches
{
    [HarmonyPostfix]
    public static void Postfix(ProtoActor actor)
    {
        if (!ModConfig.EnableStatistics.Value)
            return;

        StatisticsTracker.OnPlayerDeath(actor);
    }
}

[HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.OnPlayerRevive))]
public static class GameMainRevivePatches
{
    [HarmonyPostfix]
    public static void Postfix(ProtoActor actor)
    {
        if (!ModConfig.EnableStatistics.Value)
            return;

        StatisticsTracker.OnPlayerRevive(actor);
    }
}

[HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.OnKillCountChanged))]
public static class GameMainKillPatches
{
    [HarmonyPostfix]
    public static void Postfix(ProtoActor actor, int killCount)
    {
        if (!ModConfig.EnableStatistics.Value)
            return;

        StatisticsTracker.OnKillCountChanged(actor, killCount);
    }
}
