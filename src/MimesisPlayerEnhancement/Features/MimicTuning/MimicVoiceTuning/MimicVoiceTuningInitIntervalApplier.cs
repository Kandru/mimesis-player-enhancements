namespace MimesisPlayerEnhancement.Features.MimicTuning.MimicVoiceTuning
{
    internal static class MimicVoiceTuningInitIntervalApplier
    {
        private static readonly HashSet<int> AdjustedActorIds = new();

        private static readonly System.Reflection.FieldInfo? ContextDictField =
            AccessTools.Field(typeof(MimicVoiceSpawner), "actorIdToMimicContextDict");

        internal static void ApplyToSpawner(MimicVoiceSpawner spawner)
        {
            if (!MimicVoiceTuningResolver.ShouldApplyCustom
                || !MimicVoiceTuningResolver.TryResolveInitIntervalSeconds(out float intervalSeconds))
            {
                return;
            }

            if (ContextDictField?.GetValue(spawner) is not Dictionary<int, MimicVoiceSpawner.MimicContext> contexts)
            {
                return;
            }

            float now = GameSessionAccess.GetCurrentTickSec();
            foreach (KeyValuePair<int, MimicVoiceSpawner.MimicContext> pair in contexts)
            {
                if (!AdjustedActorIds.Add(pair.Key))
                {
                    continue;
                }

                pair.Value.NextSpawnTime = now + intervalSeconds;
            }
        }

        internal static void Clear()
        {
            AdjustedActorIds.Clear();
        }
    }
}
