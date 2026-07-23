using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace MimesisPlayerEnhancement.Features.MimicTuning.Patches
{
    internal static class MimicVoiceTuningPatches
    {
        private const string Feature = "MimicTuning";

        private static bool _loggedLastVoiceTimeMiss;

        private static readonly MethodInfo GetResponseMaxDistanceMethod =
            AccessTools.Method(
                typeof(MimicVoiceTuningResolver),
                nameof(MimicVoiceTuningResolver.GetResponseMaxDistance),
                Type.EmptyTypes);

        [HarmonyPatch(typeof(VoiceManager), nameof(VoiceManager.TrySpawnMimicReply))]
        internal static class TrySpawnMimicReplyPatch
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

                    MimicVoiceSpawner? spawner = __instance.mimicVoiceSpawner;
                    if (spawner == null)
                    {
                        __result = false;
                        return false;
                    }

                    if (!MimicVoiceTuningPatchSupport.TryGetLastMimicVoiceTime(__instance, out float lastTime))
                    {
                        if (!_loggedLastVoiceTimeMiss)
                        {
                            _loggedLastVoiceTimeMiss = true;
                            ModLog.Warn(Feature, "Last mimic voice time unavailable — custom voice tuning falls back to vanilla");
                        }

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

                    List<MimicVoiceSpawner.PreparedMimicVoiceSpawn> preparedVoices =
                        spawner.PrepareNearbyMimicReplies(actor.ActorID, actor.transform.position);
                    if (preparedVoices.Count == 0)
                    {
                        __result = false;
                        return false;
                    }

                    if (MimicVoiceTuningPatchSupport.SpawnMimicVoiceAfterDelayMethod == null)
                    {
                        return true;
                    }

                    float prevMimicVoiceTime = lastTime;
                    MimicVoiceTuningPatchSupport.SetLastMimicVoiceTime(__instance, now);

                    float delay = MimicVoiceTuningResolver.RollResponseDelaySeconds();
                    VoiceManager voiceManager = __instance;
                    MimicVoiceSpawner mimicSpawner = spawner;

                    Action spawnAction = () =>
                    {
                        bool anySpawned = false;
                        foreach (MimicVoiceSpawner.PreparedMimicVoiceSpawn item in preparedVoices)
                        {
                            if (mimicSpawner.SpawnPreparedMimicVoice(item))
                            {
                                anySpawned = true;
                            }
                        }

                        if (!anySpawned)
                        {
                            MimicVoiceTuningPatchSupport.SetLastMimicVoiceTime(voiceManager, prevMimicVoiceTime);
                        }
                    };

                    IEnumerator routine = (IEnumerator)MimicVoiceTuningPatchSupport.SpawnMimicVoiceAfterDelayMethod.Invoke(
                        __instance,
                        [delay, spawnAction])!;
                    __instance.StartCoroutine(routine);

                    __result = true;
                    return false;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"TrySpawnMimicReply prefix failed — {ex.Message}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(MimicVoiceSpawner), nameof(MimicVoiceSpawner.PrepareNearbyMimicReplies))]
        internal static class PrepareNearbyMimicRepliesTranspiler
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
                    if (!__result || speechEvent == null || !MimicVoiceTuningResolver.ShouldApplyCustom)
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

        [HarmonyPatch(typeof(MimicVoiceSpawner), nameof(MimicVoiceSpawner.SpawnPreparedMimicVoice))]
        internal static class SpawnPreparedMimicVoicePatch
        {
            [HarmonyPostfix]
            internal static void Postfix(MimicVoiceSpawner.PreparedMimicVoiceSpawn preparedVoice, bool __result)
            {
                try
                {
                    if (!__result || !MimicVoiceTuningResolver.ShouldApplyCustom)
                    {
                        return;
                    }

                    if (!MimicVoiceTuningPatchSupport.TryGetPreparedVoiceContext(preparedVoice, out MimicVoiceSpawner.MimicContext? context)
                        || context == null)
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
                        periodic: false,
                        snapshot.SpeechEvent,
                        snapshot.MimickingPlayerId,
                        snapshot.PickReason);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"SpawnPreparedMimicVoice postfix failed — {ex.Message}");
                }
            }
        }
    }
}
