namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayPatches
    {
        private const string Feature = "Ui";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(WorldOverlayPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("DestroyActor/ProtoActor", AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.DestroyActor))),
                ("OnActorDeath/ProtoActor", AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.OnActorDeath))),
                ("UpdateHp/ProtoActor", AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.UpdateHp))),
                ("UpdateConta/ProtoActor", AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.UpdateConta))),
                ("ResolvePacket_HitTargetSig/ProtoActor", AccessTools.Method(typeof(ProtoActor), "ResolvePacket_HitTargetSig")),
                ("OnPacket/ProtoActor (ActorDamagedSig)", AccessTools.Method(typeof(ProtoActor), "OnPacket", [typeof(ActorDamagedSig)])),
                ("OnPacket/GameMainBase (FieldHitTargetSig)", AccessTools.Method(typeof(GameMainBase), "OnPacket", [typeof(FieldHitTargetSig)])),
                ("OnPacket/GameMainBase (ProjectileHitTargetSig)", AccessTools.Method(typeof(GameMainBase), "OnPacket", [typeof(ProjectileHitTargetSig)])),
            ]);
        }
    }
}
