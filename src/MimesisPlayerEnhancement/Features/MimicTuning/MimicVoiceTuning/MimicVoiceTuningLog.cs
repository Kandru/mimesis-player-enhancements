namespace MimesisPlayerEnhancement.Features.MimicTuning.MimicVoiceTuning
{
    internal static class MimicVoiceTuningLog
    {
        private const string Feature = "MimicTuning";

        internal static void DebugChanceRollSkipped()
        {
            if (!ModConfig.EnableDebugLogging.Value)
            {
                return;
            }

            ModLog.Debug(Feature, "Player voice response skipped — chance roll failed");
        }

        internal static void DebugVoicePlayed(
            int mimicActorId,
            int mimicMasterId,
            bool periodic,
            SpeechEvent speechEvent,
            string mimickingPlayerId,
            string pickReason)
        {
            if (!ModConfig.EnableDebugLogging.Value || !MimicVoiceTuningResolver.ShouldApplyCustom)
            {
                return;
            }

            string mimicName = MonsterTypeLookup.GetDisplayName(mimicMasterId);
            string sourcePlayer = string.IsNullOrWhiteSpace(speechEvent.PlayerName)
                ? mimickingPlayerId
                : speechEvent.PlayerName;
            string trigger = periodic ? "ambient" : "player-response";
            string context = speechEvent.ToStringSimple();
            float duration = speechEvent.Duration;

            ModLog.Debug(
                Feature,
                $"Mimic voice played — mimic={mimicName}#{mimicActorId}, sourcePlayer={sourcePlayer}, " +
                $"clipId={speechEvent.Id}, trigger={trigger}, pick={pickReason}, context={context}, duration={duration:0.##}s");
        }
    }
}
