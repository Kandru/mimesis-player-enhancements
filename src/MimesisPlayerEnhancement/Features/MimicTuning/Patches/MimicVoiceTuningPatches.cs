using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace MimesisPlayerEnhancement.Features.MimicTuning.Patches
{
    internal static class MimicVoiceTuningPatches
    {
        private const string Feature = "MimicVoiceTuning";

        private static readonly MethodInfo RollResponseDelaySecondsMethod =
            AccessTools.Method(typeof(MimicVoiceTuningResolver), nameof(MimicVoiceTuningResolver.RollResponseDelaySeconds));

        private static readonly MethodInfo GetResponseMaxDistanceMethod =
            AccessTools.Method(typeof(MimicVoiceTuningResolver), nameof(MimicVoiceTuningResolver.GetResponseMaxDistance));

        private static readonly MethodInfo ScaleIntervalSecondsMethod =
            AccessTools.Method(typeof(MimicVoiceTuningResolver), nameof(MimicVoiceTuningResolver.ScaleIntervalSeconds));

        [HarmonyPatch(typeof(VoiceManager), nameof(VoiceManager.SpawnMimicVoicEventOnce))]
        internal static class SpawnMimicVoicEventOncePatch
        {
            [HarmonyPrefix]
            internal static bool Prefix(VoiceManager __instance, long playerUID, ref bool __result)
            {
                try
                {
                    if (!MimicVoiceTuningResolver.ShouldApplyCustom)
                    {
                        return true;
                    }

                    if (!MimicVoiceTuningPatchSupport.IsServer(__instance))
                    {
                        __result = false;
                        return false;
                    }

                    if (__instance.mimicVoiceSpawner == null)
                    {
                        __result = false;
                        return false;
                    }

                    if (!MimicVoiceTuningPatchSupport.TryGetLastMimicVoiceTime(__instance, out float lastTime))
                    {
                        return true;
                    }

                    float now = GameSessionAccess.GetCurrentTickSec();
                    if (now - lastTime < MimicVoiceTuningResolver.GetResponseCooldownSeconds())
                    {
                        __result = false;
                        return false;
                    }

                    if (!MimicVoiceTuningResolver.RollResponseChance())
                    {
                        MimicVoiceTuningLog.DebugChanceRollSkipped();
                        __result = false;
                        return false;
                    }

                    ProtoActor? actor = __instance.GetActorByPlayerUID(playerUID);
                    if (actor == null)
                    {
                        __result = false;
                        return false;
                    }

                    if (MimicVoiceTuningPatchSupport.SpawnMimicVoiceWithDelayMethod != null)
                    {
                        MimicVoiceTuningPatchSupport.SetLastMimicVoiceTime(__instance, now);
                        IEnumerator routine = (IEnumerator)MimicVoiceTuningPatchSupport.SpawnMimicVoiceWithDelayMethod.Invoke(
                            __instance,
                            [actor.ActorID, actor.transform.position])!;
                        __instance.StartCoroutine(routine);
                    }

                    __result = true;
                    return false;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"SpawnMimicVoicEventOnce prefix failed — {ex.Message}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(VoiceManager), "SpawnMimicVoiceWithDelay")]
        internal static class SpawnMimicVoiceWithDelayTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (RollResponseDelaySecondsMethod == null)
                {
                    return instructions;
                }

                List<CodeInstruction> codes = [.. instructions];
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float value && Math.Abs(value - 0.2f) < 0.0001f)
                    {
                        codes[i] = new CodeInstruction(OpCodes.Call, RollResponseDelaySecondsMethod);
                    }
                }

                return codes;
            }
        }

        [HarmonyPatch(typeof(MimicVoiceSpawner), nameof(MimicVoiceSpawner.TrySpawnMimicVoiceEventOnce))]
        internal static class TrySpawnMimicVoiceEventOnceTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (GetResponseMaxDistanceMethod == null)
                {
                    return instructions;
                }

                List<CodeInstruction> codes = [.. instructions];
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float value && Math.Abs(value - 20f) < 0.0001f)
                    {
                        codes[i] = new CodeInstruction(OpCodes.Call, GetResponseMaxDistanceMethod);
                    }
                }

                return codes;
            }
        }

        [HarmonyPatch(typeof(MimicVoiceSpawner), "PickRandomInterval")]
        internal static class PickRandomIntervalPatch
        {
            [HarmonyPostfix]
            internal static void Postfix(ref float __result)
            {
                try
                {
                    if (!MimicVoiceTuningResolver.ShouldApplyCustom)
                    {
                        return;
                    }

                    __result = MimicVoiceTuningResolver.ScaleIntervalSeconds(__result);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"PickRandomInterval postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(SpeechEventAdditionalGameData), nameof(SpeechEventAdditionalGameData.PickBestMatch))]
        internal static class PickBestMatchPatch
        {
            [HarmonyPostfix]
            internal static void Postfix(
                MimicVoiceSpawner.MimicContext context,
                bool __result,
                SpeechEvent? speechEvent,
                string mimickingPlayerID,
                string pickReason)
            {
                try
                {
                    if (!__result || speechEvent == null)
                    {
                        return;
                    }

                    MimicVoiceTuningPlaybackTrace.Record(
                        context.MimicActorID,
                        speechEvent,
                        mimickingPlayerID,
                        pickReason);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"PickBestMatch postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(MimicVoiceSpawner), "TrySpawnVoiceByContext")]
        internal static class TrySpawnVoiceByContextPatch
        {
            [HarmonyPostfix]
            internal static void Postfix(MimicVoiceSpawner.MimicContext context, bool periodic, bool __result)
            {
                try
                {
                    if (!__result || !MimicVoiceTuningResolver.ShouldApplyCustom)
                    {
                        return;
                    }

                    if (!MimicVoiceTuningPlaybackTrace.TryTake(context.MimicActorID, out MimicVoicePlaybackSnapshot snapshot))
                    {
                        return;
                    }

                    MimicVoiceTuningLog.DebugVoicePlayed(
                        context.MimicActorID,
                        context.MimicMonsterMasterID,
                        periodic,
                        snapshot.SpeechEvent,
                        snapshot.MimickingPlayerId,
                        snapshot.PickReason);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"TrySpawnVoiceByContext postfix failed — {ex.Message}");
                }
            }
        }
    }
}
