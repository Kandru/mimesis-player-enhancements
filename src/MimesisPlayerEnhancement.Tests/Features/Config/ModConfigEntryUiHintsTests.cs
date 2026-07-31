using MimesisPlayerEnhancement.Features.UserInterface;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class ModConfigEntryUiHintsTests
    {
        [Fact]
        public void ResolveInputKind_spectator_voice_balance_mode_is_select()
        {
            Assert.Equal(
                "Select",
                ModConfigEntryUiHints.ResolveInputKind(UiConfig.SectionId, "SpectatorVoiceBalanceMode"));
        }

        [Fact]
        public void ApplyToEntry_spectator_voice_balance_mode_has_three_options()
        {
            WebDashboardConfigEntryDto entry = new() { Key = "SpectatorVoiceBalanceMode" };

            ModConfigEntryUiHints.ApplyToEntry(entry, UiConfig.SectionId, "SpectatorVoiceBalanceMode");

            Assert.Equal("Select", entry.InputKind);
            Assert.Equal(
                ["Vanilla", "SpeechDucking", "StaticAttenuation"],
                entry.SelectOptions.ConvertAll(option => option.Value));
        }

        [Fact]
        public void AssignEntryGroups_spectator_voice_keys_share_group()
        {
            WebDashboardConfigSectionDto section = new()
            {
                Id = UiConfig.SectionId,
                Entries =
                [
                    new WebDashboardConfigEntryDto { Key = "SpectatorVoiceBalanceMode" },
                    new WebDashboardConfigEntryDto { Key = "SpectatorVoiceAttenuation" },
                    new WebDashboardConfigEntryDto { Key = "SpectatorVoiceDuckLevel" },
                ],
            };

            ModConfigEntryUiHints.AssignEntryGroups(section);

            string expectedGroup = $"{UiConfig.SectionId}::spectatorVoiceBalance";
            Assert.All(section.Entries, entry => Assert.Equal(expectedGroup, entry.EntryGroup));
        }
    }
}
