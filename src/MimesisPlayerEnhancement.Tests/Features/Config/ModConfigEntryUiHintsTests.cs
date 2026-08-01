using MimesisPlayerEnhancement.Features.SavegamePreparation;
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

        [Fact]
        public void AssignEntryGroups_savegame_preparation_keys_have_separate_groups()
        {
            WebDashboardConfigSectionDto section = new()
            {
                Id = SavegamePreparationConfig.SectionId,
                Entries =
                [
                    new WebDashboardConfigEntryDto { Key = "StartupMoney" },
                    new WebDashboardConfigEntryDto { Key = "StartingZone" },
                ],
            };

            ModConfigEntryUiHints.AssignEntryGroups(section);

            Assert.Equal(
                $"{SavegamePreparationConfig.SectionId}::startupMoney",
                section.Entries[0].EntryGroup);
            Assert.Equal(
                $"{SavegamePreparationConfig.SectionId}::startingZone",
                section.Entries[1].EntryGroup);
        }

        [Fact]
        public void AssignEntryGroups_more_voices_keys_share_voice_limits_and_recording_groups()
        {
            const string moreVoicesSectionId = "MimesisPlayerEnhancement_MoreVoices";
            WebDashboardConfigSectionDto section = new()
            {
                Id = moreVoicesSectionId,
                Entries =
                [
                    new WebDashboardConfigEntryDto { Key = "UnifyIndoorOutdoorVoices" },
                    new WebDashboardConfigEntryDto { Key = "MaxIndoorVoiceEvents" },
                    new WebDashboardConfigEntryDto { Key = "MaxDeathMatchVoiceEvents" },
                    new WebDashboardConfigEntryDto { Key = "MaxOutdoorVoiceEvents" },
                    new WebDashboardConfigEntryDto { Key = "RecordVoiceInMaintenance" },
                    new WebDashboardConfigEntryDto { Key = "RecordVoiceInTram" },
                    new WebDashboardConfigEntryDto { Key = "RecordVoiceDuringMimicPossession" },
                ],
            };

            ModConfigEntryUiHints.AssignEntryGroups(section);

            string voiceLimitsGroup = $"{moreVoicesSectionId}::voiceLimits";
            string voiceRecordingGroup = $"{moreVoicesSectionId}::voiceRecording";
            Assert.Equal(voiceLimitsGroup, section.Entries[0].EntryGroup);
            Assert.Equal(voiceLimitsGroup, section.Entries[1].EntryGroup);
            Assert.Equal(voiceLimitsGroup, section.Entries[2].EntryGroup);
            Assert.Equal(voiceLimitsGroup, section.Entries[3].EntryGroup);
            Assert.Equal(voiceRecordingGroup, section.Entries[4].EntryGroup);
            Assert.Equal(voiceRecordingGroup, section.Entries[5].EntryGroup);
            Assert.Equal(voiceRecordingGroup, section.Entries[6].EntryGroup);
        }
    }
}
