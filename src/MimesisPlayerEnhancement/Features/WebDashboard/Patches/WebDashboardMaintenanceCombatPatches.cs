using System.Reflection;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/IVroom.cs:L2571-2574
    [HarmonyPatch(typeof(IVroom), nameof(IVroom.DamageAppliable))]
    internal static class MaintenanceDashboardMonsterDamageAppliablePatch
    {
        [HarmonyPostfix]
        private static void Postfix(IVroom __instance, ref bool __result)
        {
            if (__result || __instance is not MaintenanceRoom)
            {
                return;
            }

            if (WebDashboardMaintenanceCombatAccess.IsDashboardSpawnedMonster(
                    WebDashboardMaintenanceCombatAccess.CurrentDamageVictim))
            {
                __result = true;
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/VWorldCombatUtil.cs:L7-34
    [HarmonyPatch(typeof(VWorldCombatUtil), nameof(VWorldCombatUtil.CalculateDamage))]
    internal static class MaintenanceDashboardMonsterCalculateDamagePatch
    {
        [HarmonyPrefix]
        private static void Prefix(VCreature victim, ref WebDashboardMaintenanceCombatAccess.DamageVictimScope __state)
        {
            __state = WebDashboardMaintenanceCombatAccess.PushDamageVictim(victim);
        }

        [HarmonyPostfix]
        private static void Postfix(ref WebDashboardMaintenanceCombatAccess.DamageVictimScope __state)
        {
            __state.Dispose();
        }
    }

    // game@0.3.1 Assembly-CSharp/StatManager.cs:L923-928
    [HarmonyPatch(typeof(StatManager), "OnChangeAbnormalStat", [typeof(ApplyMutableStatsAbnormalArgs)])]
    internal static class MaintenanceDashboardMonsterAbnormalDamagePatch
    {
        [HarmonyPrefix]
        private static void Prefix(
            StatManager __instance,
            ApplyMutableStatsAbnormalArgs mutableArgs,
            ref WebDashboardMaintenanceCombatAccess.DamageVictimScope __state)
        {
            if (mutableArgs.StatType != MutableStatType.HP)
            {
                return;
            }

            __state = WebDashboardMaintenanceCombatAccess.PushDamageVictim(__instance);
        }

        [HarmonyPostfix]
        private static void Postfix(ref WebDashboardMaintenanceCombatAccess.DamageVictimScope __state)
        {
            __state.Dispose();
        }
    }

    // game@0.3.1 Assembly-CSharp/VProjectileObject.cs:L391-446
    [HarmonyPatch(typeof(VProjectileObject), nameof(VProjectileObject.OnDamageImpl))]
    internal static class MaintenanceDashboardMonsterProjectileDamagePatch
    {
        private const string Feature = "WebDashboard";

        private static readonly FieldInfo? ParentActorField =
            AccessTools.Field(typeof(VProjectileObject), "_parentActor");

        private static readonly FieldInfo? ParentSkillContextField =
            AccessTools.Field(typeof(VProjectileObject), "_parentSkillContext");

        [HarmonyPostfix]
        private static void Postfix(
            VProjectileObject __instance,
            List<int> targetActorIDs,
            long damage,
            IsDamageImmuned isDamageImmuned,
            ref List<TargetHitInfo> __result)
        {
            if (__instance.VRoom.DamageAppliable() || damage <= 0)
            {
                return;
            }

            VCreature? parentActor = ParentActorField?.GetValue(__instance) as VCreature;
            ISkillContext? skillContext = ParentSkillContextField?.GetValue(__instance) as ISkillContext;
            int skillMasterId = skillContext?.SkillMasterID ?? 0;

            foreach (int targetId in targetActorIDs)
            {
                VActor? actor = __instance.VRoom.FindActorByObjectID(targetId);
                if (!WebDashboardMaintenanceCombatAccess.IsDashboardSpawnedMonster(actor))
                {
                    continue;
                }

                if (HasAppliedDamage(__result, targetId))
                {
                    continue;
                }

                if (isDamageImmuned(actor!).Immuned)
                {
                    continue;
                }

                try
                {
                    actor!.StatControlUnit?.ApplyDamage(new ApplyDamageArgs(
                        parentActor,
                        actor,
                        MutableStatChangeCause.ActiveAttack,
                        damage,
                        0L,
                        skillMasterId));
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Maintenance projectile damage failed — {ex.Message}");
                }
            }
        }

        private static bool HasAppliedDamage(List<TargetHitInfo> hits, int targetId)
        {
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].targetID == targetId && hits[i].damage > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
