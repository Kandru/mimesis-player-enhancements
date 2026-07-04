namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class FunnyTramMenuLabels
    {
        private static readonly string[] LabelKeys =
        [
            "saveslots.funny_tram_01",
            "saveslots.funny_tram_02",
            "saveslots.funny_tram_03",
            "saveslots.funny_tram_04",
            "saveslots.funny_tram_05",
            "saveslots.funny_tram_06",
            "saveslots.funny_tram_07",
            "saveslots.funny_tram_08",
            "saveslots.funny_tram_09",
            "saveslots.funny_tram_10",
            "saveslots.funny_tram_11",
            "saveslots.funny_tram_12",
            "saveslots.funny_tram_13",
            "saveslots.funny_tram_14",
            "saveslots.funny_tram_15",
            "saveslots.funny_tram_16",
            "saveslots.funny_tram_17",
            "saveslots.funny_tram_18",
            "saveslots.funny_tram_19",
            "saveslots.funny_tram_20",
            "saveslots.funny_tram_21",
            "saveslots.funny_tram_22",
            "saveslots.funny_tram_23",
            "saveslots.funny_tram_24",
            "saveslots.funny_tram_25",
            "saveslots.funny_tram_26",
            "saveslots.funny_tram_27",
            "saveslots.funny_tram_28",
            "saveslots.funny_tram_29",
            "saveslots.funny_tram_30",
        ];

        internal static string PickRandom()
        {
            return Util.ModL10n.Get(LabelKeys[UnityEngine.Random.Range(0, LabelKeys.Length)]);
        }
    }
}
