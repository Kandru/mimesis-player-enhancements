using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

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

        [HarmonyPatch(typeof(MimicVoiceSpawner), "Awake")]
        internal static class MimicVoiceSpawnerAwakePostfix
        {
            [HarmonyPostfix]
            internal static void Postfix(MimicVoiceSpawner __instance)
            {
                try
                {
                    MimicVoiceTuningPatchSupport.ApplyMinRequiredSpeechs(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"MimicVoiceSpawner Awake postfix failed — {ex.Message}");
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

        [HarmonyPatch(typeof(MimicVoiceSpawner), "TryPickHeuristicSpeechEvent")]
        internal static class TryPickHeuristicSpeechEventTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (MimicVoiceTuningPatchSupport.GetSpeakAudienceRangeMetersMethod == null)
                {
                    return instructions;
                }

                List<CodeInstruction> codes = [.. instructions];
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float value && Math.Abs(value - 15f) < 0.0001f)
                    {
                        codes[i] = new CodeInstruction(
                            OpCodes.Call,
                            MimicVoiceTuningPatchSupport.GetSpeakAudienceRangeMetersMethod);
                    }
                }

                return codes;
            }
        }

        [HarmonyPatch(typeof(SpeechEventArchive), "ObserverRpcPlayOnActor")]
        internal static class ObserverRpcPlayOnActorMutePrefix
        {
            [HarmonyPrefix]
            internal static void Prefix(ref bool muteLocalPlayerVoice)
            {
                try
                {
                    muteLocalPlayerVoice = MimicVoiceTuningResolver.ResolveMuteLocalPlayerVoice(muteLocalPlayerVoice);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ObserverRpcPlayOnActor mute prefix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(MimicVoiceSpawner), "AddOrRemoveContexts")]
        internal static class AddOrRemoveContextsPostfix
        {
            [HarmonyPostfix]
            internal static void Postfix(MimicVoiceSpawner __instance)
            {
                try
                {
                    MimicVoiceTuningInitIntervalApplier.ApplyToSpawner(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"AddOrRemoveContexts postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(MimicVoiceSpawner), "PickRandomInterval")]
        internal static class PickRandomIntervalPatch
        {
            [HarmonyPrefix]
            internal static bool Prefix(
                bool periodic,
                MMMimicMonsterTable.Row monsterTableRow,
                SpeechType_Area speechType_Area,
                ref float __result)
            {
                try
                {
                    if (!MimicVoiceTuningResolver.ShouldApplyCustom)
                    {
                        return true;
                    }

                    if (!periodic)
                    {
                        if (MimicVoiceTuningResolver.TryResolvePostReplyIntervalSeconds(out float postReplySeconds))
                        {
                            __result = postReplySeconds;
                            return false;
                        }

                        return true;
                    }

                    if (MimicVoiceTuningPatchSupport.IsDeathMatchArea(speechType_Area)
                        && MimicVoiceTuningResolver.TryResolveDeathMatchIntervalSeconds(out float deathMatchSeconds))
                    {
                        __result = Mathf.Max(0f, deathMatchSeconds);
                        return false;
                    }

                    if (!MimicVoiceTuningPatchSupport.IsDeathMatchArea(speechType_Area)
                        && MimicVoiceTuningResolver.TryResolvePeriodicIntervalSeconds(out float periodicSeconds))
                    {
                        __result = Mathf.Max(0f, periodicSeconds);
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"PickRandomInterval prefix failed — {ex.Message}");
                    return true;
                }
            }

            [HarmonyPostfix]
            internal static void Postfix(bool periodic, ref float __result)
            {
                try
                {
                    if (!periodic || !MimicVoiceTuningResolver.ShouldApplyCustom)
                    {
                        return;
                    }

                    __result = MimicVoiceTuningResolver.ScalePeriodicIntervalSeconds(__result);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"PickRandomInterval postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(SpeechEventAdditionalGameData), nameof(SpeechEventAdditionalGameData.PickBestMatch))]
        internal static class PickBestMatchTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                IEnumerable<CodeInstruction> codes = ReplaceStaticIntervalField(
                    instructions,
                    MimicVoiceTuningPatchSupport.PlayTimeIntervalField,
                    MimicVoiceTuningPatchSupport.GetClipReuseCooldownSecondsMethod);
                return ReplaceStaticIntervalField(
                    codes,
                    MimicVoiceTuningPatchSupport.DeathMatchPlayTimeIntervalField,
                    MimicVoiceTuningPatchSupport.GetDeathMatchClipReuseCooldownSecondsMethod);
            }
        }

        [HarmonyPatch(typeof(SpeechEventAdditionalGameData), nameof(SpeechEventAdditionalGameData.PickBestMatch))]
        internal static class PickBestMatchPostfix
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

        private static IEnumerable<CodeInstruction> ReplaceStaticIntervalField(
            IEnumerable<CodeInstruction> instructions,
            FieldInfo? field,
            MethodInfo? replacementMethod)
        {
            if (field == null || replacementMethod == null)
            {
                return instructions;
            }

            List<CodeInstruction> codes = [.. instructions];
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldsfld && ReferenceEquals(codes[i].operand, field))
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, replacementMethod);
                }
            }

            return codes;
        }
    }
}
