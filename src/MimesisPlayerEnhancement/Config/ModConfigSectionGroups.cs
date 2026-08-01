namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Nested settings-nav groups (Client / Session / Balance / World) for the web dashboard.
    /// </summary>
    internal static class ModConfigSectionGroups
    {
        internal const string Client = "client";
        internal const string Session = "session";
        internal const string Balance = "balance";
        internal const string World = "world";

        private static readonly string[] GroupOrder = [Client, Session, Balance, World];

        private static readonly Dictionary<string, string> SectionToGroup =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [ModConfigRegistry.MainSectionId] = Client,
                ["MimesisPlayerEnhancement_Ui"] = Client,
                ["MimesisPlayerEnhancement_Privacy"] = Client,

                ["MimesisPlayerEnhancement_SavegamePreparation"] = Session,
                ["MimesisPlayerEnhancement_MorePlayers"] = Session,
                ["MimesisPlayerEnhancement_JoinAnytime"] = Session,
                ["MimesisPlayerEnhancement_MoreVoices"] = Session,
                ["MimesisPlayerEnhancement_Persistence"] = Session,
                ["MimesisPlayerEnhancement_PlayerAnnouncements"] = Session,
                ["MimesisPlayerEnhancement_Statistics"] = Session,

                ["MimesisPlayerEnhancement_SpawnScaling"] = Balance,
                ["MimesisPlayerEnhancement_LootMultiplicator"] = Balance,
                ["MimesisPlayerEnhancement_Economy"] = Balance,
                ["MimesisPlayerEnhancement_DungeonTime"] = Balance,

                ["MimesisPlayerEnhancement_MimicTuning"] = World,
                ["MimesisPlayerEnhancement_PlayerTuning"] = World,
                ["MimesisPlayerEnhancement_DungeonRandomizer"] = World,
                ["MimesisPlayerEnhancement_Weather"] = World,
            };

        /// <summary>
        /// Preferred settings section order (flattened group membership). Excludes Web Dashboard.
        /// Within each group, sections are A–Z by English title; Savegame Preparation stays first in Session.
        /// </summary>
        internal static readonly string[] PreferredSectionOrder =
        [
            // Client: General, Privacy, User Interface
            ModConfigRegistry.MainSectionId,
            "MimesisPlayerEnhancement_Privacy",
            "MimesisPlayerEnhancement_Ui",

            // Session: Prep first, then A–Z
            "MimesisPlayerEnhancement_SavegamePreparation",
            "MimesisPlayerEnhancement_JoinAnytime",
            "MimesisPlayerEnhancement_MorePlayers",
            "MimesisPlayerEnhancement_MoreVoices",
            "MimesisPlayerEnhancement_Persistence",
            "MimesisPlayerEnhancement_PlayerAnnouncements",
            "MimesisPlayerEnhancement_Statistics",

            // Balance: A–Z
            "MimesisPlayerEnhancement_DungeonTime",
            "MimesisPlayerEnhancement_Economy",
            "MimesisPlayerEnhancement_LootMultiplicator",
            "MimesisPlayerEnhancement_SpawnScaling",

            // World: A–Z
            "MimesisPlayerEnhancement_DungeonRandomizer",
            "MimesisPlayerEnhancement_MimicTuning",
            "MimesisPlayerEnhancement_PlayerTuning",
            "MimesisPlayerEnhancement_Weather",
        ];

        internal static IReadOnlyList<string> GetGroupOrder() => GroupOrder;

        internal static bool TryGetGroupId(string sectionId, out string groupId)
        {
            return SectionToGroup.TryGetValue(sectionId, out groupId!);
        }
    }
}
