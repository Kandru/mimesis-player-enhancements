namespace MimesisPlayerEnhancement.Features.PlayerTuning.Patches
{
    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor.cs:L4561-4575
    [HarmonyPatch(typeof(ProtoActor), "SetControlMode")]
    internal static class ProtoActorSetControlModePatch
    {
        private const string Feature = "PlayerTuning";

        [HarmonyPostfix]
        public static void Postfix(ProtoActor __instance)
        {
            if (!PlayerTuningResolver.DisablePlayerCollision)
            {
                return;
            }

            try
            {
                PlayerTuningCollision.OnPassThroughActorConfigured(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SetControlMode postfix failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor.cs:L4212-4234
    [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.SetAsOtherPlayer))]
    internal static class ProtoActorSetAsOtherPlayerPatch
    {
        private const string Feature = "PlayerTuning";

        [HarmonyPostfix]
        public static void Postfix(ProtoActor __instance)
        {
            if (!PlayerTuningResolver.DisablePlayerCollision)
            {
                return;
            }

            try
            {
                PlayerTuningCollision.OnPassThroughActorConfigured(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SetAsOtherPlayer postfix failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor.cs:L4236-4254
    [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.SetAsMonster))]
    internal static class ProtoActorSetAsMonsterPatch
    {
        private const string Feature = "PlayerTuning";

        [HarmonyPostfix]
        public static void Postfix(ProtoActor __instance)
        {
            if (!PlayerTuningResolver.DisablePlayerCollision)
            {
                return;
            }

            try
            {
                PlayerTuningCollision.OnPassThroughActorConfigured(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SetAsMonster postfix failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor.cs:L4678-4694
    [HarmonyPatch(typeof(ProtoActor), "SetupMonsterCapsuleCollider")]
    internal static class ProtoActorSetupMonsterCapsuleColliderPatch
    {
        private const string Feature = "PlayerTuning";

        [HarmonyPostfix]
        public static void Postfix(ProtoActor __instance, int masterID)
        {
            if (!PlayerTuningResolver.DisablePlayerCollision)
            {
                return;
            }

            try
            {
                PlayerTuningCollision.OnPassThroughActorConfigured(__instance, masterID);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SetupMonsterCapsuleCollider postfix failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/Mimic.Actors/ProtoActor.cs:L6908-6950
    [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.OnActorRevive))]
    internal static class ProtoActorOnActorRevivePatch
    {
        private const string Feature = "PlayerTuning";

        [HarmonyPostfix]
        public static void Postfix(ProtoActor __instance)
        {
            if (!PlayerTuningResolver.DisablePlayerCollision)
            {
                return;
            }

            try
            {
                PlayerTuningCollision.OnPassThroughActorConfigured(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnActorRevive postfix failed — {ex.Message}");
            }
        }
    }
}
