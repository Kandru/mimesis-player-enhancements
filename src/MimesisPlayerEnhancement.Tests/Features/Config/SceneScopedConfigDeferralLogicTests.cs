using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class SceneScopedConfigDeferralLogicTests
    {
        private const string EconomySection = "MimesisPlayerEnhancement_Economy";
        private const string EconomyToggleKey = "EnableEconomy";

        private static ModConfigChangeInfo SectionChange(string sectionId, string key) =>
            new()
            {
                ChangedKeys =
                [
                    new ModConfigKeyChange { SectionId = sectionId, Key = key },
                ],
            };

        [Theory]
        [InlineData("MorePlayers")]
        [InlineData("JoinAnytime")]
        [InlineData("MimicTuning")]
        public void ShouldDefer_returns_false_for_non_gated_modules(string moduleName)
        {
            bool result = SceneScopedConfigDeferralLogic.ShouldDefer(
                moduleName,
                SectionChange(EconomySection, "StartupMoneyMultiplier"),
                isGameplaySceneActive: true,
                isMasterEnabled: true,
                masterToggleKey: EconomyToggleKey);

            Assert.False(result);
        }

        [Theory]
        [InlineData("Economy")]
        [InlineData("LootMultiplicator")]
        [InlineData("SpawnScaling")]
        [InlineData("DungeonTime")]
        [InlineData("DungeonRandomizer")]
        public void ShouldDefer_returns_false_outside_gameplay_scene(string moduleName)
        {
            string sectionId = $"MimesisPlayerEnhancement_{moduleName}";

            bool result = SceneScopedConfigDeferralLogic.ShouldDefer(
                moduleName,
                SectionChange(sectionId, "SomeKey"),
                isGameplaySceneActive: false,
                isMasterEnabled: true,
                masterToggleKey: "EnableFeature");

            Assert.False(result);
        }

        [Fact]
        public void ShouldDefer_returns_true_for_gated_module_section_change_during_gameplay()
        {
            bool result = SceneScopedConfigDeferralLogic.ShouldDefer(
                "Economy",
                SectionChange(EconomySection, "StartupMoneyMultiplier"),
                isGameplaySceneActive: true,
                isMasterEnabled: true,
                masterToggleKey: EconomyToggleKey);

            Assert.True(result);
        }

        [Theory]
        [InlineData("Economy")]
        [InlineData("LootMultiplicator")]
        [InlineData("SpawnScaling")]
        [InlineData("DungeonTime")]
        [InlineData("DungeonRandomizer")]
        public void ShouldDefer_returns_true_on_full_reload_during_gameplay(string moduleName)
        {
            bool result = SceneScopedConfigDeferralLogic.ShouldDefer(
                moduleName,
                ModConfigChangeInfo.FullReload,
                isGameplaySceneActive: true,
                isMasterEnabled: true,
                masterToggleKey: "EnableFeature");

            Assert.True(result);
        }

        [Fact]
        public void ShouldDefer_returns_false_when_master_toggle_disabled_and_toggle_key_changed()
        {
            bool result = SceneScopedConfigDeferralLogic.ShouldDefer(
                "Economy",
                SectionChange(EconomySection, EconomyToggleKey),
                isGameplaySceneActive: true,
                isMasterEnabled: false,
                masterToggleKey: EconomyToggleKey);

            Assert.False(result);
        }

        [Fact]
        public void ShouldDefer_returns_true_when_master_disabled_but_unrelated_key_changed()
        {
            bool result = SceneScopedConfigDeferralLogic.ShouldDefer(
                "Economy",
                SectionChange(EconomySection, "StartupMoneyMultiplier"),
                isGameplaySceneActive: true,
                isMasterEnabled: false,
                masterToggleKey: EconomyToggleKey);

            Assert.True(result);
        }

        [Fact]
        public void ShouldDefer_returns_false_when_change_affects_different_section()
        {
            bool result = SceneScopedConfigDeferralLogic.ShouldDefer(
                "Economy",
                SectionChange("MimesisPlayerEnhancement_MorePlayers", "MaxPlayers"),
                isGameplaySceneActive: true,
                isMasterEnabled: true,
                masterToggleKey: EconomyToggleKey);

            Assert.False(result);
        }

        [Fact]
        public void IsModuleAffected_returns_true_for_full_reload()
        {
            Assert.True(SceneScopedConfigDeferralLogic.IsModuleAffected("Economy", ModConfigChangeInfo.FullReload));
        }

        [Fact]
        public void IsModuleAffected_matches_section_case_insensitively()
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

            Assert.True(SceneScopedConfigDeferralLogic.IsModuleAffected("Economy", change));
        }

        [Fact]
        public void IsMasterToggleDisabledChange_returns_true_on_full_reload_when_master_disabled()
        {
            bool result = SceneScopedConfigDeferralLogic.IsMasterToggleDisabledChange(
                "Economy",
                ModConfigChangeInfo.FullReload,
                isMasterEnabled: false,
                masterToggleKey: EconomyToggleKey);

            Assert.True(result);
        }

        [Fact]
        public void IsMasterToggleDisabledChange_returns_false_when_master_enabled()
        {
            bool result = SceneScopedConfigDeferralLogic.IsMasterToggleDisabledChange(
                "Economy",
                SectionChange(EconomySection, EconomyToggleKey),
                isMasterEnabled: true,
                masterToggleKey: EconomyToggleKey);

            Assert.False(result);
        }

        [Fact]
        public void IsMasterToggleDisabledChange_returns_true_when_toggle_key_matches()
        {
            bool result = SceneScopedConfigDeferralLogic.IsMasterToggleDisabledChange(
                "Economy",
                SectionChange(EconomySection, EconomyToggleKey),
                isMasterEnabled: false,
                masterToggleKey: EconomyToggleKey);

            Assert.True(result);
        }

        [Fact]
        public void IsMasterToggleDisabledChange_returns_false_for_non_gated_module()
        {
            bool result = SceneScopedConfigDeferralLogic.IsMasterToggleDisabledChange(
                "MorePlayers",
                SectionChange("MimesisPlayerEnhancement_MorePlayers", "EnableMorePlayers"),
                isMasterEnabled: false,
                masterToggleKey: "EnableMorePlayers");

            Assert.False(result);
        }
    }
}
