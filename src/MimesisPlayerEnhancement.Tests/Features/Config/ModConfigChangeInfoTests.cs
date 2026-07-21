using MimesisPlayerEnhancement;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class ModConfigChangeInfoTests
    {
        [Fact]
        public void AffectsSection_returns_true_for_full_reload()
        {
            Assert.True(ModConfigChangeInfo.FullReload.AffectsSection("MimesisPlayerEnhancement_Economy"));
        }

        [Fact]
        public void AffectsSection_matches_section_case_insensitively()
        {
            ModConfigChangeInfo change = new()
            {
                ChangedKeys =
                [
                    new ModConfigKeyChange
                    {
                        SectionId = "mimesisplayerenhancement_economy",
                        Key = "StartupMoneyMultiplier",
                    },
                ],
            };

            Assert.True(change.AffectsSection("MimesisPlayerEnhancement_Economy"));
            Assert.False(change.AffectsSection("MimesisPlayerEnhancement_MorePlayers"));
        }

        [Fact]
        public void ChangedSections_returns_distinct_section_ids()
        {
            ModConfigChangeInfo change = new()
            {
                ChangedKeys =
                [
                    new ModConfigKeyChange { SectionId = "MimesisPlayerEnhancement_Economy", Key = "A" },
                    new ModConfigKeyChange { SectionId = "MimesisPlayerEnhancement_Economy", Key = "B" },
                    new ModConfigKeyChange { SectionId = "MimesisPlayerEnhancement_LootMultiplicator", Key = "C" },
                ],
            };

            string[] sections = change.ChangedSections.ToArray();

            Assert.Equal(2, sections.Length);
            Assert.Contains("MimesisPlayerEnhancement_Economy", sections);
            Assert.Contains("MimesisPlayerEnhancement_LootMultiplicator", sections);
        }
    }
}
