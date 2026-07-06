using System;
using System.Reflection;
using System.Reflection.Emit;
using Bifrost.Cooked;

namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandleReinforceItem))]
    internal static class VPlayerHandleReinforceItemPatch
    {
        private const string Feature = "Economy";

        private static readonly FieldInfo UpgradeCostField =
            AccessTools.Field(typeof(ItemEquipmentInfo), nameof(ItemEquipmentInfo.UpgradeCost))
            ?? throw new InvalidOperationException("ItemEquipmentInfo.UpgradeCost not found");

        private static readonly MethodInfo ScaleReinforceCostMethod =
            AccessTools.Method(typeof(EconomyApplier), nameof(EconomyApplier.ScaleReinforceCost))
            ?? throw new InvalidOperationException("EconomyApplier.ScaleReinforceCost not found");

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            LocalVariableInfo? maintenanceRoomLocal = FindMaintenanceRoomLocal(original);
            if (maintenanceRoomLocal == null)
            {
                ModLog.Warn(Feature, "HandleReinforceItem transpiler skipped — MaintenanceRoom local not found");
                return instructions;
            }

            List<CodeInstruction> codes = [.. instructions];
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldfld
                    || !ReferenceEquals(codes[i].operand, UpgradeCostField))
                {
                    continue;
                }

                codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldloc, maintenanceRoomLocal.LocalIndex));
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, ScaleReinforceCostMethod));
                i += 2;
            }

            return codes;
        }

        private static LocalVariableInfo? FindMaintenanceRoomLocal(MethodBase method)
        {
            MethodBody? body = method.GetMethodBody();
            if (body == null)
            {
                return null;
            }

            foreach (LocalVariableInfo local in body.LocalVariables)
            {
                if (local.LocalType == typeof(MaintenanceRoom))
                {
                    return local;
                }
            }

            return null;
        }
    }
}
