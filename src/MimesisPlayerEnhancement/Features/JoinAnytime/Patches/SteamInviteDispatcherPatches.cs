using System.Reflection;
using System.Reflection.Emit;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/SteamInviteDispatcher.cs:L786-798
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.CreateLobby), typeof(bool), typeof(bool))]
    internal static class SteamInviteDispatcherCreateLobbyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SteamInviteDispatcher __instance, bool isOpenForRandomMatch, bool isRetryAttempt)
        {
            JoinAnytimeLobbyController.OnLobbyCreated(__instance, isOpenForRandomMatch, isRetryAttempt);
        }
    }

    // game@0.3.1 Assembly-CSharp/SteamInviteDispatcher.cs:L441-454
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyPublic))]
    internal static class SteamInviteDispatcherSetLobbyPublicPatch
    {
        private const string Feature = "JoinAnytime";

        [HarmonyPrefix]
        private static bool Prefix(bool isPublic)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (isPublic)
            {
                JoinAnytimeLobbyController.SetHostWantsPublicMatchmaking(true);
            }
            else if (JoinAnytimeLobbyController.ShouldBlockPublicRoomClose())
            {
                ModLog.Debug(Feature, "Blocked SetLobbyPublic(false) for join-anytime host.");
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(SteamInviteDispatcher __instance, bool isPublic)
        {
            JoinAnytimeLobbyController.OnSetLobbyPublicCompleted(__instance, isPublic);
        }
    }

    // game@0.3.1 Assembly-CSharp/SteamInviteDispatcher.cs:L456-511
    [HarmonyPatch]
    internal static class SteamInviteDispatcherSetLobbyPublicCoroutineTranspiler
    {
        private static readonly MethodInfo CoercePublicRoomWriteFlagMethod =
            AccessTools.Method(
                typeof(JoinAnytimePublicLobbyTools),
                nameof(JoinAnytimePublicLobbyTools.CoercePublicRoomWriteFlag),
                [typeof(bool), typeof(bool)]);

        private static MethodBase? TargetMethod() =>
            HarmonyPatchHelper.GetEnumeratorMoveNext(
                typeof(SteamInviteDispatcher),
                "SetLobbyPublicCoroutine",
                [typeof(bool)]);

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase original)
        {
            FieldInfo? isPublicField = ResolveIsPublicField(original.DeclaringType);
            if (isPublicField == null)
            {
                return instructions;
            }

            List<CodeInstruction> codes = [.. instructions];
            MethodInfo? toStringMethod = AccessTools.Method(typeof(bool), nameof(bool.ToString), []);

            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call || codes[i].operand is not MethodInfo calledMethod)
                {
                    continue;
                }

                if (calledMethod != toStringMethod)
                {
                    continue;
                }

                OpCode loadLocalOpcode = codes[i - 1].opcode;
                if (loadLocalOpcode != OpCodes.Ldloc && loadLocalOpcode != OpCodes.Ldloc_0
                    && loadLocalOpcode != OpCodes.Ldloc_1 && loadLocalOpcode != OpCodes.Ldloc_2
                    && loadLocalOpcode != OpCodes.Ldloc_3 && loadLocalOpcode != OpCodes.Ldloc_S)
                {
                    continue;
                }

                CodeInstruction loadFlag = codes[i - 1];
                codes[i - 1] = new CodeInstruction(OpCodes.Ldarg_0);
                codes.Insert(i, new CodeInstruction(OpCodes.Ldfld, isPublicField));
                codes.Insert(i + 1, loadFlag);
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, CoercePublicRoomWriteFlagMethod));
                break;
            }

            return codes;
        }

        private static FieldInfo? ResolveIsPublicField(Type? stateMachineType)
        {
            if (stateMachineType == null)
            {
                return null;
            }

            FieldInfo? field = AccessTools.Field(stateMachineType, "isPublic");
            if (field != null)
            {
                return field;
            }

            return AccessTools.Field(stateMachineType, "<>3__isPublic");
        }
    }

    // game@0.3.1 Assembly-CSharp/SteamInviteDispatcher.cs:L513-516
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.UpdateLobbyData))]
    internal static class SteamInviteDispatcherUpdateLobbyDataPatch
    {
        private const string Feature = "JoinAnytime";

        [HarmonyPrefix]
        private static bool Prefix(SteamInviteDispatcher __instance, string key, string value)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (string.Equals(key, SteamInviteDispatcher.IS_PUBLIC_KEY, StringComparison.Ordinal)
                && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                && JoinAnytimeHub.IsHost())
            {
                JoinAnytimeLobbyController.SetHostWantsPublicMatchmaking(true);
            }

            if (string.Equals(key, SteamInviteDispatcher.IS_PUBLIC_KEY, StringComparison.Ordinal)
                && string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
                && JoinAnytimeLobbyController.ShouldBlockPublicRoomClose())
            {
                ModLog.Debug(Feature, "Blocked PublicRoom=false lobby data update for join-anytime host.");
                JoinAnytimeLobbyController.ApplyHostPublicLobbyIntent();
                return false;
            }

            return true;
        }
    }

    // game@0.3.1 Assembly-CSharp/SteamInviteDispatcher.cs:L961-968
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetPresenceInLobby))]
    internal static class SteamInviteDispatcherSetLobbyPublicPresencePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(SteamInviteDispatcher __instance)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (JoinAnytimeHub.IsHostLobbyPublic(__instance))
            {
                JoinAnytimeLobbyController.ApplyLobbyPresence(__instance, wantsPublic: true);
                return false;
            }

            return true;
        }
    }

    // game@0.3.1 Assembly-CSharp/SteamInviteDispatcher.cs:L988-995
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetPresencePlaying))]
    internal static class SteamInviteDispatcherSetPresencePlayingPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(SteamInviteDispatcher __instance)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (JoinAnytimeHub.IsHostLobbyPublic(__instance))
            {
                JoinAnytimeLobbyController.ApplyLobbyPresence(__instance, wantsPublic: true);
                JoinAnytimeLobbyController.RefreshLobbyState(force: true);
                return false;
            }

            return true;
        }
    }

    // game@0.3.1 Assembly-CSharp/SteamInviteDispatcher.cs:L1015-1041
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.UpdatePlayerGroupSize))]
    internal static class SteamInviteDispatcherUpdatePlayerGroupSizePatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref int playerCount)
        {
            if (!ModConfig.EnableJoinAnytime.Value || !JoinAnytimeHub.IsHost())
            {
                return;
            }

            int sessionCount = JoinAnytimeRoomTools.GetSessionPlayerCount();
            playerCount = JoinAnytimeLobbyDisplay.GetBrowsePlayerCount(sessionCount);
        }
    }
}
