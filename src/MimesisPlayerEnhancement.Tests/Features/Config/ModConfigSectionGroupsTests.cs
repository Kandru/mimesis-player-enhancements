using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class ModConfigSectionGroupsTests
    {
        [Fact]
        public void PreferredSectionOrder_matches_group_membership_and_order()
        {
            string? previousGroup = null;
            foreach (string sectionId in ModConfigSectionGroups.PreferredSectionOrder)
            {
                Assert.True(
                    ModConfigSectionGroups.TryGetGroupId(sectionId, out string groupId),
                    $"Missing group for {sectionId}");

                if (previousGroup != null && previousGroup != groupId)
                {
                    int previousIndex = IndexOfGroup(previousGroup);
                    int currentIndex = IndexOfGroup(groupId);
                    Assert.True(
                        currentIndex > previousIndex,
                        $"Group order regression: {previousGroup} -> {groupId}");
                }

                previousGroup = groupId;
            }
        }

        [Fact]
        public void PreferredSectionOrder_covers_all_grouped_sections_exactly_once()
        {
            Assert.Equal(
                ModConfigSectionGroups.PreferredSectionOrder.Length,
                ModConfigSectionGroups.PreferredSectionOrder.Distinct(StringComparer.OrdinalIgnoreCase).Count());

            foreach (string sectionId in ModConfigSectionGroups.PreferredSectionOrder)
            {
                Assert.False(
                    ModConfigRegistry.IsWebDashboardSection(sectionId),
                    "Web Dashboard must not appear in preferred settings order");
            }
        }

        [Theory]
        [InlineData(ModConfigRegistry.MainSectionId, ModConfigSectionGroups.Client)]
        [InlineData("MimesisPlayerEnhancement_Ui", ModConfigSectionGroups.Client)]
        [InlineData("MimesisPlayerEnhancement_Privacy", ModConfigSectionGroups.Client)]
        [InlineData("MimesisPlayerEnhancement_SavegamePreparation", ModConfigSectionGroups.Session)]
        [InlineData("MimesisPlayerEnhancement_MorePlayers", ModConfigSectionGroups.Session)]
        [InlineData("MimesisPlayerEnhancement_JoinAnytime", ModConfigSectionGroups.Session)]
        [InlineData("MimesisPlayerEnhancement_SpawnScaling", ModConfigSectionGroups.Balance)]
        [InlineData("MimesisPlayerEnhancement_LootMultiplicator", ModConfigSectionGroups.Balance)]
        [InlineData("MimesisPlayerEnhancement_Economy", ModConfigSectionGroups.Balance)]
        [InlineData("MimesisPlayerEnhancement_DungeonTime", ModConfigSectionGroups.Balance)]
        [InlineData("MimesisPlayerEnhancement_MimicTuning", ModConfigSectionGroups.World)]
        [InlineData("MimesisPlayerEnhancement_Weather", ModConfigSectionGroups.World)]
        public void TryGetGroupId_maps_expected_sections(string sectionId, string expectedGroup)
        {
            Assert.True(ModConfigSectionGroups.TryGetGroupId(sectionId, out string groupId));
            Assert.Equal(expectedGroup, groupId);
        }

        [Fact]
        public void PreferredSectionOrder_is_alphabetical_within_groups_with_prep_first()
        {
            Dictionary<string, string> englishTitles = new(StringComparer.OrdinalIgnoreCase)
            {
                [ModConfigRegistry.MainSectionId] = "General",
                ["MimesisPlayerEnhancement_Privacy"] = "Privacy",
                ["MimesisPlayerEnhancement_Ui"] = "User Interface",
                ["MimesisPlayerEnhancement_SavegamePreparation"] = "Savegame Preparation",
                ["MimesisPlayerEnhancement_JoinAnytime"] = "Join Anytime",
                ["MimesisPlayerEnhancement_MorePlayers"] = "More Players",
                ["MimesisPlayerEnhancement_MoreVoices"] = "More Voices",
                ["MimesisPlayerEnhancement_Persistence"] = "Persistence",
                ["MimesisPlayerEnhancement_PlayerAnnouncements"] = "Player Announcements",
                ["MimesisPlayerEnhancement_Statistics"] = "Statistics",
                ["MimesisPlayerEnhancement_DungeonTime"] = "Dungeon Time",
                ["MimesisPlayerEnhancement_Economy"] = "Economy",
                ["MimesisPlayerEnhancement_LootMultiplicator"] = "Loot Multiplier",
                ["MimesisPlayerEnhancement_SpawnScaling"] = "Spawn Scaling",
                ["MimesisPlayerEnhancement_DungeonRandomizer"] = "Dungeon Randomizer",
                ["MimesisPlayerEnhancement_MimicTuning"] = "Mimic Tuning",
                ["MimesisPlayerEnhancement_PlayerTuning"] = "Player Tuning",
                ["MimesisPlayerEnhancement_Weather"] = "Weather",
            };

            foreach (string groupId in ModConfigSectionGroups.GetGroupOrder())
            {
                List<string> sectionIds = ModConfigSectionGroups.PreferredSectionOrder
                    .Where(id => ModConfigSectionGroups.TryGetGroupId(id, out string gid)
                        && string.Equals(gid, groupId, StringComparison.Ordinal))
                    .ToList();

                Assert.NotEmpty(sectionIds);

                List<string> expected = [.. sectionIds];
                if (string.Equals(groupId, ModConfigSectionGroups.Session, StringComparison.Ordinal))
                {
                    Assert.Equal("MimesisPlayerEnhancement_SavegamePreparation", sectionIds[0]);
                    expected =
                    [
                        "MimesisPlayerEnhancement_SavegamePreparation",
                        .. sectionIds.Skip(1)
                            .OrderBy(id => englishTitles[id], StringComparer.OrdinalIgnoreCase),
                    ];
                }
                else
                {
                    expected = [.. sectionIds.OrderBy(id => englishTitles[id], StringComparer.OrdinalIgnoreCase)];
                }

                Assert.Equal(expected, sectionIds);
            }
        }

        private static int IndexOfGroup(string groupId)
        {
            IReadOnlyList<string> order = ModConfigSectionGroups.GetGroupOrder();
            for (int i = 0; i < order.Count; i++)
            {
                if (string.Equals(order[i], groupId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
