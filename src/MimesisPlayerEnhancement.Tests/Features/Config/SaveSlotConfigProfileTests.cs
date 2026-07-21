using MimesisPlayerEnhancement;
using MimesisPlayerEnhancement.Config.QuickSettings;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class SaveSlotConfigProfileTests
    {
        [Fact]
        public void Parse_reads_quick_mode_with_preset_metadata()
        {
            SparseTomlConfig.Document doc = SparseTomlConfig.Load("""
                [MimesisPlayerEnhancement_Profile]
                Mode = quick
                PresetId = builtin-easy
                PresetRevision = 2
                """);

            SaveConfigProfileState profile = SaveSlotConfigProfile.Parse(doc);

            Assert.Equal(SaveConfigProfileMode.Quick, profile.Mode);
            Assert.Equal("builtin-easy", profile.PresetId);
            Assert.Equal(2, profile.PresetRevision);
        }

        [Fact]
        public void Parse_infers_custom_from_legacy_gameplay_overrides()
        {
            SparseTomlConfig.Document doc = SparseTomlConfig.Load("""
                [MimesisPlayerEnhancement_Economy]
                EnableEconomy = true
                """);

            SaveConfigProfileState profile = SaveSlotConfigProfile.Parse(doc);

            Assert.Equal(SaveConfigProfileMode.Custom, profile.Mode);
        }

        [Fact]
        public void Parse_infers_global_when_no_overrides()
        {
            SaveConfigProfileState profile = SaveSlotConfigProfile.Parse(new SparseTomlConfig.Document());

            Assert.Equal(SaveConfigProfileMode.Global, profile.Mode);
        }

        [Fact]
        public void Parse_quick_without_preset_id_becomes_custom()
        {
            SparseTomlConfig.Document doc = SparseTomlConfig.Load("""
                [MimesisPlayerEnhancement_Profile]
                Mode = quick
                """);

            SaveConfigProfileState profile = SaveSlotConfigProfile.Parse(doc);

            Assert.Equal(SaveConfigProfileMode.Custom, profile.Mode);
        }

        [Fact]
        public void HasGameplayOverrides_ignores_profile_section()
        {
            SparseTomlConfig.Document doc = SparseTomlConfig.Load("""
                [MimesisPlayerEnhancement_Profile]
                Mode = global
                """);

            Assert.False(SaveSlotConfigProfile.HasGameplayOverrides(doc));
        }

        [Fact]
        public void WriteProfileSection_writes_custom_mode_and_removes_quick_keys()
        {
            SparseTomlConfig.Document doc = new();
            SaveSlotConfigProfile.WriteProfileSection(doc, new SaveConfigProfileState
            {
                Mode = SaveConfigProfileMode.Custom,
            });

            Assert.True(doc.Sections.ContainsKey("MimesisPlayerEnhancement_Profile"));
            Dictionary<string, string> keys = doc.Sections["MimesisPlayerEnhancement_Profile"];
            Assert.Equal("custom", keys["Mode"]);
            Assert.False(keys.ContainsKey("PresetId"));
            Assert.False(keys.ContainsKey("PresetRevision"));
        }

        [Fact]
        public void RemoveProfileSection_clears_profile_metadata()
        {
            SparseTomlConfig.Document doc = SparseTomlConfig.Load("""
                [MimesisPlayerEnhancement_Profile]
                Mode = custom
                """);

            SaveSlotConfigProfile.RemoveProfileSection(doc);

            Assert.False(doc.Sections.ContainsKey("MimesisPlayerEnhancement_Profile"));
            Assert.DoesNotContain("MimesisPlayerEnhancement_Profile", doc.SectionOrder);
        }
    }
}
