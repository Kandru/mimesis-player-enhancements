namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class VoiceEventContext
    {
        internal static bool IsDeathMatch(SpeechType_Area area) => area == SpeechType_Area.DeathMatch;

        internal static bool IsOutdoorArea(SpeechType_Area area) =>
            area == SpeechType_Area.Outdoor || area == SpeechType_Area.Tram;

        internal static bool IsTrapOrMonster(SpeechEvent evt)
        {
            SpeechEventAdditionalGameData? gameData = evt.GameData;
            if (gameData == null)
            {
                return false;
            }

            if (IsTrapOrMonsterArea(gameData.Area))
            {
                return true;
            }

            if (gameData.Monsters != null && gameData.Monsters.Count > 0)
            {
                return true;
            }

            List<IncomingEvent>? incoming = gameData.IncomingEventStart;
            if (incoming == null || incoming.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < incoming.Count; i++)
            {
                if (IsTrapOrMonsterIncomingType(incoming[i].EventType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTrapOrMonsterArea(SpeechType_Area area) =>
            area is SpeechType_Area.TrapCorridor
                or SpeechType_Area.GrabbedByMonster
                or SpeechType_Area.TrapWeight
                or SpeechType_Area.TrapHole
                or SpeechType_Area.BearTrapING
                or SpeechType_Area.ElectricShockING
                or SpeechType_Area.MimicGrabSkill;

        private static bool IsTrapOrMonsterIncomingType(SpeechEvent_IncomingType type) =>
            type is SpeechEvent_IncomingType.Monster
                or SpeechEvent_IncomingType.GrabSkill
                or SpeechEvent_IncomingType.BearTrapped
                or SpeechEvent_IncomingType.InvisibleMine
                or SpeechEvent_IncomingType.TimeBombWarning;
    }
}
